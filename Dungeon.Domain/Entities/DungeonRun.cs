using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Entities;

/// <summary>
/// One exploration of a generated dungeon by a Player game session (ADR-GLOB-011).
/// Only what changes during play is stored; the dungeon itself is regenerated from the
/// seed, the settings and the generator version.
/// </summary>
public sealed class DungeonRun
{
    private readonly List<RunMob> _mobs = [];
    private List<int> _discoveredRoomIds = [];
    private byte[] _visitedTiles = [];

    // Required by EF Core.
    private DungeonRun() { }

    public Guid Id { get; private set; }

    public Guid GameSessionId { get; private set; }

    public Seed Seed { get; private set; }

    public int GeneratorVersion { get; private set; }

    public int RoomCount { get; private set; }

    public int FloorCount { get; private set; }

    public DungeonRunStatus Status { get; private set; }

    public int CurrentFloor { get; private set; }

    public int HeroX { get; private set; }

    public int HeroY { get; private set; }

    /// <summary>Incremented by every accepted action: the game is turn-based.</summary>
    public int Turn { get; private set; }

    /// <summary>
    /// Whether the boss of the current floor is defeated, which opens the gate down to the
    /// next floor. Reset on arrival at a new floor.
    /// </summary>
    public bool IsFloorBossDefeated { get; private set; }

    /// <summary>
    /// Chests that this run has already opened. Their position and content remain deterministic
    /// in the generated map; this collection is the mutable per-run overlay.
    /// </summary>
    public List<OpenedChest> OpenedChests { get; private set; } = [];

    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    /// The mobs of the current floor, dead ones included, in element order: the order they act
    /// in. Sorted here because the database returns its rows in no particular order.
    /// </summary>
    public IReadOnlyList<RunMob> Mobs => [.. _mobs.OrderBy(mob => mob.ElementId)];

    /// <summary>The rooms of the current floor a hero of the party has set foot in.</summary>
    public IReadOnlyList<int> DiscoveredRoomIds => _discoveredRoomIds;

    /// <summary>
    /// The tiles of the current floor a hero of the party has stood on, one bit per tile in
    /// row order: tile <c>i = y * width + x</c> is bit <c>i % 8</c> of byte <c>i / 8</c>. The
    /// client rebuilds its fog of war from them. Reset on arrival at a new floor.
    /// </summary>
    public ReadOnlyMemory<byte> VisitedTiles => _visitedTiles;

    public Position HeroPosition => new(HeroX, HeroY);

    public DungeonSettings Settings => new(RoomCount, FloorCount);

    public static DungeonRun Start(
        Guid runId,
        Guid gameSessionId,
        GeneratedDungeon dungeon,
        DateTimeOffset startedAt
    )
    {
        Position entrance = dungeon.Floors[0].Entrance;

        DungeonRun run = new()
        {
            Id = runId,
            GameSessionId = gameSessionId,
            Seed = dungeon.Seed,
            GeneratorVersion = dungeon.GeneratorVersion,
            RoomCount = dungeon.Settings.RoomCount,
            FloorCount = dungeon.Settings.FloorCount,
            Status = DungeonRunStatus.Active,
            CurrentFloor = 0,
            HeroX = entrance.X,
            HeroY = entrance.Y,
            Turn = 0,
            StartedAt = startedAt,
        };
        run.ArriveOn(dungeon.Floors[0]);

        return run;
    }

    /// <summary>
    /// Moves the hero exactly one tile. Walls, obstacles, the void, anything outside the floor
    /// and the gate of a boss still standing are rejected; the run is left unchanged when a
    /// move is refused. Walking into the open gate takes the party down: the hero arrives at
    /// the entrance of the next floor. Once the hero has moved, the mobs of the floor take
    /// their turn.
    /// </summary>
    /// <returns>Where the hero stands after the move, on the current floor.</returns>
    public Position MoveHero(Direction direction, GeneratedDungeon dungeon, MobRules mobRules)
    {
        DungeonFloor floor = GetCurrentFloor(dungeon);
        Position target = HeroPosition.Step(direction);

        if (!floor.Contains(target))
        {
            throw new InvalidMoveException(target, "the tile is outside the dungeon");
        }

        CellType cell = floor.GetCell(target);
        if (cell == CellType.Gate)
        {
            return GoDown(target, floor, dungeon);
        }

        if (!cell.IsWalkable())
        {
            throw new InvalidMoveException(target, $"a {cell} tile is not walkable");
        }

        if (_mobs.Any(mob => mob.IsActive && mob.Position == target))
        {
            throw new InvalidMoveException(target, "a mob stands there");
        }

        HeroX = target.X;
        HeroY = target.Y;
        Visit(target, floor);
        MobTurn.Play(this, floor, mobRules);
        Turn++;

        return target;
    }

    private Position GoDown(Position gate, DungeonFloor floor, GeneratedDungeon dungeon)
    {
        if (!IsFloorBossDefeated)
        {
            throw new InvalidMoveException(
                gate,
                "the gate stays locked until the boss of this floor is defeated"
            );
        }

        if (floor.IsFinalFloor)
        {
            throw new InvalidMoveException(gate, "there is no floor below the last one");
        }

        CurrentFloor++;
        ArriveOn(dungeon.Floors[CurrentFloor]);
        Position entrance = dungeon.Floors[CurrentFloor].Entrance;
        HeroX = entrance.X;
        HeroY = entrance.Y;
        IsFloorBossDefeated = false;
        Turn++;

        return entrance;
    }

    /// <summary>
    /// Records the victory over the boss of the current floor: the gate down opens, and
    /// defeating the boss of the last floor wins the run. The fight itself belongs to
    /// Combat; the hero must be in the boss room. Recording it twice changes nothing.
    /// </summary>
    public void DefeatFloorBoss(GeneratedDungeon dungeon)
    {
        DungeonFloor floor = GetCurrentFloor(dungeon);
        if (IsFloorBossDefeated)
        {
            return;
        }

        Room bossRoom = floor.Rooms.Single(room => room.Type == RoomType.Boss);
        if (floor.GetRoomId(HeroPosition) != bossRoom.Id)
        {
            throw new BossNotInReachException(Id, CurrentFloor);
        }

        IsFloorBossDefeated = true;
        Turn++;

        if (floor.IsFinalFloor)
        {
            Status = DungeonRunStatus.Won;
        }
    }

    /// <summary>
    /// Opens the chest represented by an <see cref="ElementType.Item"/> on the current floor.
    /// The party must stand on the chest tile or one of its eight neighbouring tiles. Retrying a
    /// successfully opened chest is harmless:
    /// it returns <see langword="false"/> without consuming another turn.
    /// </summary>
    public bool OpenChest(int elementId, GeneratedDungeon dungeon, DateTimeOffset openedAt)
    {
        DungeonFloor floor = GetCurrentFloor(dungeon);
        DungeonElement? chest = floor.Elements.SingleOrDefault(element =>
            element.Id == elementId && element.Type == ElementType.Item
        );

        if (chest is null)
        {
            throw new ChestNotFoundException(Id, CurrentFloor, elementId);
        }

        if (HeroPosition.DistanceTo(chest.Position) > 1)
        {
            throw new ChestNotInReachException(Id, CurrentFloor, elementId);
        }

        if (
            OpenedChests.Any(opened =>
                opened.Floor == CurrentFloor && opened.ElementId == elementId
            )
        )
        {
            return false;
        }

        OpenedChests.Add(new OpenedChest(Id, CurrentFloor, elementId, openedAt));
        Turn++;
        return true;
    }

    private DungeonFloor GetCurrentFloor(GeneratedDungeon dungeon)
    {
        if (Status != DungeonRunStatus.Active)
        {
            throw new DungeonRunNotActiveException(Id, Status);
        }

        if (dungeon.Seed != Seed || dungeon.GeneratorVersion != GeneratorVersion)
        {
            throw new ArgumentException(
                $"Dungeon {dungeon.Seed} v{dungeon.GeneratorVersion} is not the dungeon of run '{Id}'.",
                nameof(dungeon)
            );
        }

        return dungeon.Floors[CurrentFloor];
    }

    /// <summary>Whether a hero of the party has stood on this tile of the current floor.</summary>
    public bool HasVisited(Position tile, DungeonFloor floor)
    {
        int index = tile.Y * floor.Width + tile.X;
        return floor.Contains(tile)
            && index / 8 < _visitedTiles.Length
            && (_visitedTiles[index / 8] & (1 << (index % 8))) != 0;
    }

    private void Visit(Position tile, DungeonFloor floor)
    {
        if (HasVisited(tile, floor))
        {
            return;
        }

        // A new array rather than a bit flipped in place, so that EF Core sees the change.
        // A run started before the tiles were recorded has none yet.
        int size = (floor.Width * floor.Height + 7) / 8;
        byte[] tiles = _visitedTiles.Length == size ? [.. _visitedTiles] : new byte[size];
        int index = tile.Y * floor.Width + tile.X;
        tiles[index / 8] |= (byte)(1 << (index % 8));
        _visitedTiles = tiles;
    }

    internal void Discover(int roomId)
    {
        if (!_discoveredRoomIds.Contains(roomId))
        {
            _discoveredRoomIds.Add(roomId);
        }
    }

    /// <summary>
    /// The mobs of the floor the party arrives on take over from those of the floor above:
    /// one per enemy element, on its spawn tile. Only the arrival room is discovered, and only
    /// the entrance visited.
    /// </summary>
    private void ArriveOn(DungeonFloor floor)
    {
        _mobs.Clear();
        _mobs.AddRange(
            floor.Elements.Where(element => element.Type == ElementType.Enemy).Select(RunMob.Spawn)
        );
        _discoveredRoomIds = floor.GetRoomId(floor.Entrance) is int room ? [room] : [];
        _visitedTiles = [];
        Visit(floor.Entrance, floor);
    }
}
