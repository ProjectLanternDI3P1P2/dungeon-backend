using System.Text;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Services.Generation;
using Dungeon.Domain.Services.Randomness;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Test.TestSupport;

/// <summary>Shared seeds, dungeons and helpers for the dungeon tests.</summary>
public static class DungeonTestData
{
    public static readonly Seed ReferenceSeed = Seed.Parse("0KX4M2T9QZ7PA");

    /// <summary>A fixed but varied set of seeds: the tests stay deterministic.</summary>
    public static IEnumerable<Seed> SampleSeeds(int count, ulong origin = 20260928)
    {
        ulong state = origin;
        for (int index = 0; index < count; index++)
        {
            yield return new Seed(SplitMix64.Next(ref state));
        }
    }

    public static TheoryData<ulong> FiftySeeds()
    {
        TheoryData<ulong> data = [];
        foreach (Seed seed in SampleSeeds(50))
        {
            data.Add(seed.Value);
        }

        return data;
    }

    public static GeneratedDungeon Generate(Seed seed, DungeonSettings? settings = null)
    {
        return new DungeonGenerator().Generate(seed, settings ?? DungeonSettings.Default);
    }

    /// <summary>
    /// Everything the generator decides, as text: tiles (walls, doors, obstacles, gates),
    /// rooms (type, bounds, depth, connections) and elements (type, position, room, kind).
    /// Two dungeons are identical exactly when their snapshots are equal.
    /// </summary>
    public static string Snapshot(GeneratedDungeon dungeon)
    {
        StringBuilder builder = new();
        builder.Append(
            $"{dungeon.Seed}|v{dungeon.GeneratorVersion}|{dungeon.Settings.RoomCount}/{dungeon.Settings.FloorCount}"
        );

        foreach (DungeonFloor floor in dungeon.Floors)
        {
            builder.Append('\n').Append(SnapshotFloor(floor));
        }

        return builder.ToString();
    }

    public static string SnapshotFloor(DungeonFloor floor)
    {
        StringBuilder builder = new();
        builder.Append(
            $"floor {floor.Index} {floor.Width}x{floor.Height} entrance {floor.Entrance}\n"
        );

        for (int y = 0; y < floor.Height; y++)
        {
            for (int x = 0; x < floor.Width; x++)
            {
                builder.Append((int)floor.GetCell(new Position(x, y)));
            }

            builder.Append('\n');
        }

        foreach (Room room in floor.Rooms)
        {
            builder.Append(
                $"room {room.Id} {room.Type} {room.GridCell} {room.Interior} {room.Center} d{room.Depth} [{string.Join(',', room.ConnectedRoomIds)}]\n"
            );
        }

        foreach (DungeonElement element in floor.Elements)
        {
            builder.Append(
                $"element {element.Id} {element.Type} {element.Position} r{element.RoomId} k{element.Kind}\n"
            );
        }

        return builder.ToString();
    }

    /// <summary>64-bit FNV-1a of the snapshot: short enough to pin in a golden-master test.</summary>
    public static string Fingerprint(GeneratedDungeon dungeon)
    {
        ulong hash = 14695981039346656037;
        foreach (char character in Snapshot(dungeon))
        {
            hash = unchecked((hash ^ character) * 1099511628211);
        }

        return hash.ToString("x16");
    }

    public static IEnumerable<Position> PositionsOf(DungeonFloor floor, CellType cellType)
    {
        for (int y = 0; y < floor.Height; y++)
        {
            for (int x = 0; x < floor.Width; x++)
            {
                Position position = new(x, y);
                if (floor.GetCell(position) == cellType)
                {
                    yield return position;
                }
            }
        }
    }

    /// <summary>Breadth-first search over walkable tiles: the directions to follow, or null.</summary>
    public static List<Direction>? FindPath(DungeonFloor floor, Position from, Position to)
    {
        Dictionary<Position, (Position Previous, Direction Direction)> cameFrom = new();
        HashSet<Position> visited = [from];
        Queue<Position> queue = new([from]);

        while (queue.Count > 0)
        {
            Position current = queue.Dequeue();
            if (current == to)
            {
                List<Direction> path = [];
                while (current != from)
                {
                    (Position previous, Direction direction) = cameFrom[current];
                    path.Add(direction);
                    current = previous;
                }

                path.Reverse();
                return path;
            }

            foreach (Direction direction in Enum.GetValues<Direction>())
            {
                Position next = current.Step(direction);
                if (floor.IsWalkable(next) && visited.Add(next))
                {
                    cameFrom[next] = (current, direction);
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    /// <summary>Every walkable tile reachable from <paramref name="from"/>, one tile at a time.</summary>
    public static HashSet<Position> ReachableFrom(DungeonFloor floor, Position from)
    {
        HashSet<Position> visited = [from];
        Queue<Position> queue = new([from]);

        while (queue.Count > 0)
        {
            Position current = queue.Dequeue();
            foreach (Direction direction in Enum.GetValues<Direction>())
            {
                Position next = current.Step(direction);
                if (floor.IsWalkable(next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited;
    }

    /// <summary>Walks the hero of the run to the target along a shortest path, one move a turn.</summary>
    public static void WalkTo(DungeonRun run, GeneratedDungeon dungeon, Position target)
    {
        DungeonFloor floor = dungeon.Floors[run.CurrentFloor];
        foreach (Direction direction in FindPath(floor, run.HeroPosition, target)!)
        {
            run.MoveHero(direction, dungeon, MobRules.Default);
        }
    }

    /// <summary>A hand-made floor, for tests that need an exact tile next to the hero.</summary>
    public static GeneratedDungeon SingleRowDungeon(params CellType[] cells)
    {
        DungeonFloor floor = new(
            index: 0,
            isFinalFloor: true,
            width: cells.Length,
            height: 1,
            cells,
            rooms: [],
            elements: [],
            entrance: new Position(0, 0)
        );

        return new GeneratedDungeon(
            ReferenceSeed,
            DungeonGenerator.CurrentVersion,
            new DungeonSettings(DungeonSettings.MinimumRoomsPerFloor, 1),
            [floor]
        );
    }

    /// <summary>
    /// A hand-made last floor for the mob tests: <c>#</c> wall, <c>.</c> floor, <c>+</c> door,
    /// <c>t</c> trap, <c>H</c> where the hero arrives, <c>z</c> <c>m</c> <c>g</c> <c>b</c> a
    /// zombie, mage, goblin or bat. The first room is the start room, the others are combat
    /// rooms; an element belongs to the room that holds it.
    /// </summary>
    public static GeneratedDungeon Arena(string[] rows, params RoomBounds[] rooms)
    {
        int width = rows[0].Length;
        int height = rows.Length;
        CellType[] cells = new CellType[width * height];
        List<DungeonElement> elements = [];
        Position entrance = new(0, 0);

        int RoomOf(Position position) =>
            Array.FindIndex(rooms, room => room.Contains(position)) is int index and >= 0
                ? index
                : throw new ArgumentException($"{position} lies in no room.");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Position position = new(x, y);
                char tile = rows[y][x];
                cells[y * width + x] = tile switch
                {
                    '#' => CellType.Wall,
                    '+' => CellType.Door,
                    _ => CellType.Floor,
                };

                EnemyKind? kind = tile switch
                {
                    'z' => EnemyKind.Zombie,
                    'm' => EnemyKind.Mage,
                    'g' => EnemyKind.Goblin,
                    'b' => EnemyKind.Bat,
                    _ => null,
                };
                if (kind != null)
                {
                    elements.Add(
                        new DungeonElement(
                            Guid.Parse($"00000000-0000-0000-0000-{elements.Count + 1:000000000000}"),
                            ElementType.Enemy,
                            position,
                            RoomOf(position),
                            kind
                        )
                    );
                }
                else if (tile == 't')
                {
                    elements.Add(
                        new DungeonElement(
                            Guid.Parse($"00000000-0000-0000-0000-{elements.Count + 1:000000000000}"),
                            ElementType.Trap,
                            position,
                            RoomOf(position)
                        )
                    );
                }
                else if (tile == 'H')
                {
                    entrance = position;
                }
            }
        }

        Room[] floorRooms =
        [
            .. rooms.Select(
                (bounds, id) =>
                    new Room(
                        id,
                        id == 0 ? RoomType.Start : RoomType.Combat,
                        new Position(id, 0),
                        bounds,
                        new Position(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2),
                        id,
                        []
                    )
            ),
        ];

        DungeonFloor floor = new(
            index: 0,
            isFinalFloor: true,
            width,
            height,
            cells,
            floorRooms,
            elements,
            entrance
        );

        return new GeneratedDungeon(
            ReferenceSeed,
            DungeonGenerator.CurrentVersion,
            new DungeonSettings(DungeonSettings.MinimumRoomsPerFloor, 1),
            [floor]
        );
    }
}
