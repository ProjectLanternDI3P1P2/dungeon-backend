using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Services.Randomness;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Services.Generation;

/// <summary>
/// A debug dungeon behind a reserved seed, on every floor: a start room in the south, a bare
/// room north of it, then every easter-egg room further north, all in one column joined by
/// straight corridors. Mobs are spawned in the bare rooms by hand
/// (<see cref="DungeonRun.SpawnMob"/>) to watch how each kind moves; the easter-egg rooms
/// show their decorations without waiting for the legendary roll.
/// </summary>
public static class SandboxDungeon
{
    public static readonly Seed Seed = Seed.Parse("0000000SANDBX");

    private const int InteriorWidth = 21;
    private const int InteriorHeight = 15;
    private const int CorridorLength = 6;

    // Void around the walls: the client draws wall faces and rims there.
    private const int Margin = 3;

    public static GeneratedDungeon Build(DungeonSettings settings, int generatorVersion)
    {
        return new GeneratedDungeon(
            Seed,
            generatorVersion,
            settings,
            [
                .. Enumerable
                    .Range(0, settings.FloorCount)
                    .Select(index => BuildFloor(index, index == settings.FloorCount - 1)),
            ]
        )
        {
            IsSandbox = true,
        };
    }

    private static DungeonFloor BuildFloor(int index, bool isFinalFloor)
    {
        // From south to north; ids follow this order, so the start room is 0.
        List<RoomTemplate?> stack =
        [
            null,
            null,
            .. RoomTemplates.All.Where(template => template.RoomTypes.Contains(RoomType.Secret)),
        ];
        int width = Margin + 1 + InteriorWidth + Margin + 1;
        int column = Margin + 1 + InteriorWidth / 2;

        RoomBounds[] bounds = LayOut(stack, column);
        int height = bounds[0].Bottom + Margin + 2;
        CellType[] cells = new CellType[width * height];
        DeterministicRandom elementIds = DeterministicRandom.ForStream(
            Seed,
            RandomStream.ElementIds,
            index
        );
        List<DungeonElement> elements = [];
        List<Room> rooms = [];

        for (int id = 0; id < stack.Count; id++)
        {
            Dictionary<char, Position> firstSpots = Stamp(cells, width, bounds[id], stack[id]);
            AddDecorations(elements, elementIds, id, firstSpots);

            // The corridor up to the next room north.
            if (id + 1 < stack.Count)
            {
                CarveCorridor(cells, width, column, bounds[id + 1], bounds[id]);
            }

            rooms.Add(BuildRoom(id, stack, bounds[id], column));
        }

        RaiseWalls(cells, width, height);
        return new DungeonFloor(
            index,
            isFinalFloor,
            new GridSize(width, height),
            cells,
            rooms,
            elements,
            rooms[0].Center
        );
    }

    /// <summary>The interior of every room of the stack, laid out from the north down.</summary>
    private static RoomBounds[] LayOut(List<RoomTemplate?> stack, int column)
    {
        RoomBounds[] bounds = new RoomBounds[stack.Count];
        int top = Margin + 1;
        for (int id = stack.Count - 1; id >= 0; id--)
        {
            (int roomWidth, int roomHeight) = SizeOf(stack[id]);
            bounds[id] = new RoomBounds(column - roomWidth / 2, top, roomWidth, roomHeight);
            top = bounds[id].Bottom + CorridorLength + 3;
        }

        return bounds;
    }

    /// <summary>The size of a room: that of its template, or of a bare room.</summary>
    private static (int Width, int Height) SizeOf(RoomTemplate? template)
    {
        return template is null
            ? (InteriorWidth, InteriorHeight)
            : (template.Width, template.Height);
    }

    /// <summary>Draws a room on the tiles, bare or from its template; its spots, by first tile.</summary>
    private static Dictionary<char, Position> Stamp(
        CellType[] cells,
        int width,
        RoomBounds room,
        RoomTemplate? template
    )
    {
        Dictionary<char, Position> firstSpots = [];
        for (int y = 0; y < room.Height; y++)
        {
            for (int x = 0; x < room.Width; x++)
            {
                char tile = template?.At(x, y, mirrored: false) ?? RoomTemplate.FloorTile;
                Position position = new(room.X + x, room.Y + y);
                cells[position.Y * width + position.X] = RoomTemplate.CellTypeOf(tile);
                firstSpots.TryAdd(tile, position);
            }
        }

        return firstSpots;
    }

    /// <summary>One element per decoration of the room, on the first tile of its footprint.</summary>
    private static void AddDecorations(
        List<DungeonElement> elements,
        DeterministicRandom elementIds,
        int roomId,
        Dictionary<char, Position> firstSpots
    )
    {
        foreach (char spot in RoomTemplate.DecorationSpots)
        {
            if (firstSpots.TryGetValue(spot, out Position at))
            {
                elements.Add(
                    new DungeonElement(
                        elementIds.NextGuid(),
                        ElementType.Decoration,
                        at,
                        roomId,
                        Decoration: RoomTemplate.DecorationOf(spot)
                    )
                );
            }
        }
    }

    /// <summary>The corridor from a room up to the one north of it, with a door at each end.</summary>
    private static void CarveCorridor(
        CellType[] cells,
        int width,
        int column,
        RoomBounds north,
        RoomBounds south
    )
    {
        for (int y = north.Bottom + 2; y < south.Y - 1; y++)
        {
            cells[y * width + column] = CellType.Floor;
        }

        cells[(north.Bottom + 1) * width + column] = CellType.Door;
        cells[(south.Y - 1) * width + column] = CellType.Door;
    }

    private static Room BuildRoom(
        int id,
        List<RoomTemplate?> stack,
        RoomBounds interior,
        int column
    )
    {
        RoomType type = id switch
        {
            0 => RoomType.Start,
            1 => RoomType.Empty,
            _ => RoomType.Secret,
        };
        RoomLayout layout = new(
            new Position(0, stack.Count - 1 - id),
            interior,
            new Position(column, interior.Y + interior.Height / 2)
        );
        return new Room(id, type, layout, id, ConnectionsOf(id, stack.Count), stack[id]?.EasterEgg);
    }

    /// <summary>The rooms next to a room of the stack: the start room only knows the one north of it.</summary>
    private static int[] ConnectionsOf(int id, int count)
    {
        if (id == 0)
        {
            return [1];
        }

        return id + 1 < count ? [id - 1, id + 1] : [id - 1];
    }

    /// <summary>Every void tile touching a room, a door or the corridor, diagonals included, becomes a wall.</summary>
    private static void RaiseWalls(CellType[] cells, int width, int height)
    {
        bool IsInside(int x, int y) =>
            x >= 0
            && y >= 0
            && x < width
            && y < height
            && cells[y * width + x] is not (CellType.Void or CellType.Wall);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (
                    cells[y * width + x] == CellType.Void
                    && Enumerable
                        .Range(0, 9)
                        .Any(around => IsInside(x + around % 3 - 1, y + around / 3 - 1))
                )
                {
                    cells[y * width + x] = CellType.Wall;
                }
            }
        }
    }
}
