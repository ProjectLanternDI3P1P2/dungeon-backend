using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Services.Generation;
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
    private readonly List<RunParticipant> _participants = [];
    private readonly List<RunEncounter> _encounters = [];
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

    /// <summary>The heroes of the party. Empty for a run started without Player (sandbox, dev).</summary>
    public IReadOnlyList<RunParticipant> Participants => _participants;

    /// <summary>Every encounter of the run, settled ones included.</summary>
    public IReadOnlyList<RunEncounter> Encounters => _encounters;

    /// <summary>The fight in progress, if any: the party is blocked and the mobs wait.</summary>
    public RunEncounter? OpenEncounter =>
        _encounters.SingleOrDefault(encounter => encounter.IsOpen);

    /// <summary>
    /// The encounter to show: the open one, or the one settled on this very turn (shown once,
    /// so that the client sees the outcome), otherwise none.
    /// </summary>
    public RunEncounter? VisibleEncounter =>
        OpenEncounter ?? _encounters.SingleOrDefault(encounter => encounter.ResolvedOnTurn == Turn);

    public DungeonSettings Settings => new(RoomCount, FloorCount);

    public static DungeonRun Start(
        Guid runId,
        Guid gameSessionId,
        GeneratedDungeon dungeon,
        DateTimeOffset startedAt,
        IEnumerable<Guid>? heroIds = null
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
        foreach (Guid heroId in (heroIds ?? []).Distinct())
        {
            run._participants.Add(RunParticipant.Join(heroId));
        }

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
    /// <param name="encounterRules">
    /// How contacts become fights; Stub (contact kills the mob) when omitted, and for a run
    /// without participants: Combat needs a hero.
    /// </param>
    public Position MoveHero(
        Direction direction,
        GeneratedDungeon dungeon,
        MobRules mobRules,
        EncounterRules? encounterRules = null
    )
    {
        DungeonFloor floor = GetCurrentFloor(dungeon);
        if (OpenEncounter is RunEncounter fight)
        {
            throw new EncounterInProgressException(Id, fight.Id);
        }

        EncounterRules? combat =
            encounterRules is { Mode: EncounterMode.Combat } && _participants.Count > 0
                ? encounterRules
                : null;
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

        if (
            combat is not null
            && !IsFloorBossDefeated
            && floor
                .GetElementsAt(target)
                .FirstOrDefault(element => element.Type == ElementType.Boss)
                is DungeonElement boss
        )
        {
            // Walking into the boss starts its fight; the hero stays where it stands.
            List<RunMob> escort =
            [
                .. _mobs.Where(mob =>
                    mob.IsActive && mob.Position.DistanceTo(HeroPosition) <= combat.EngageRadius
                ),
            ];
            StartEncounter(EncounterTrigger.Boss, escort, boss.Id, floor, combat, Turn + 1);
            Turn++;
            return HeroPosition;
        }

        if (_mobs.FirstOrDefault(mob => mob.IsActive && mob.Position == target) is RunMob blocker)
        {
            if (combat is null)
            {
                throw new InvalidMoveException(target, "a mob stands there");
            }

            // A step into a mob is a contact: the hero stays, the mob and those in reach fight.
            List<RunMob> pack =
            [
                blocker,
                .. _mobs.Where(mob =>
                    mob.IsActive
                    && mob != blocker
                    && mob.Position.DistanceTo(HeroPosition) <= combat.EngageRadius
                ),
            ];
            StartEncounter(EncounterTrigger.Collision, pack, null, floor, combat, Turn + 1);
            Turn++;
            return HeroPosition;
        }

        HeroX = target.X;
        HeroY = target.Y;
        Visit(target, floor);
        MobTurn.Play(this, floor, mobRules, combat);
        Turn++;

        return target;
    }

    /// <summary>
    /// Freezes the pack of a contact into one encounter: the nearest mobs first, then by
    /// element id, at most <see cref="EncounterRules.MaxPackSize"/>. The mobs left out stay
    /// active; nobody dies until Combat reports the outcome.
    /// </summary>
    internal void StartEncounter(
        EncounterTrigger trigger,
        IReadOnlyCollection<RunMob> engaged,
        Guid? bossElementId,
        DungeonFloor floor,
        EncounterRules rules,
        int turn
    )
    {
        Position hero = HeroPosition;
        int room = bossElementId is null ? rules.MaxPackSize : Math.Max(0, rules.MaxPackSize - 1);
        List<RunMob> pack =
        [
            .. engaged
                .Distinct()
                .OrderBy(mob => mob.Position.DistanceTo(hero))
                .ThenBy(mob => mob.ElementId)
                .Take(room),
        ];
        foreach (RunMob mob in pack)
        {
            mob.Engage();
        }

        _encounters.Add(
            RunEncounter.Start(
                trigger,
                CurrentFloor,
                floor.GetRoomId(hero),
                [.. pack.Select(mob => mob.ElementId)],
                bossElementId,
                turn
            )
        );
    }

    /// <summary>Combat opened the fight of the encounter. Idempotent.</summary>
    public void MarkEncounterCombatStarted(Guid encounterId, Guid combatId) =>
        RequireEncounter(encounterId).CombatStarted(combatId);

    /// <summary>Combat could not be reached: the encounter waits for a retry, the party stays blocked.</summary>
    public void MarkEncounterCombatUnavailable(Guid encounterId) =>
        RequireEncounter(encounterId).CombatUnavailable();

    /// <summary>
    /// Applies the outcome Combat reported. Won: the pack dies (the boss is defeated for a
    /// boss fight). Lost: the run is lost. Fled: the defeated mobs die, the others survive
    /// and rest on their next turn. The heroes keep the hit points and mana they ended with.
    /// Reporting the same fight twice changes nothing.
    /// </summary>
    public void ResolveEncounter(
        Guid encounterId,
        Guid combatId,
        EncounterOutcome outcome,
        IReadOnlyCollection<HeroFinalState> heroes,
        IReadOnlyCollection<Guid> defeatedElementIds,
        GeneratedDungeon dungeon
    )
    {
        RunEncounter encounter = RequireEncounter(encounterId);
        if (encounter.Status == EncounterStatus.Resolved)
        {
            if (encounter.CombatId != combatId)
            {
                throw new EncounterNotFoundException(
                    Id,
                    encounterId,
                    $"it was settled by combat '{encounter.CombatId}', not '{combatId}'"
                );
            }

            return;
        }

        if (encounter.CombatId is Guid known && known != combatId)
        {
            throw new EncounterNotFoundException(
                Id,
                encounterId,
                $"its fight is combat '{known}', not '{combatId}'"
            );
        }

        Turn++;
        HashSet<Guid> defeated = [.. defeatedElementIds];
        foreach (RunMob mob in _mobs.Where(mob => encounter.MobElementIds.Contains(mob.ElementId)))
        {
            bool dies =
                outcome == EncounterOutcome.Won
                || (outcome == EncounterOutcome.Fled && defeated.Contains(mob.ElementId));
            if (dies && mob.IsActive)
            {
                mob.Kill(Turn);
            }
            else
            {
                mob.Release(skipNextTurn: outcome == EncounterOutcome.Fled);
            }
        }

        foreach (HeroFinalState hero in heroes)
        {
            RunParticipant? participant = _participants.SingleOrDefault(p =>
                p.HeroId == hero.HeroId
            );
            // A hero knocked out in a fight the party won gets up with 1 HP.
            participant?.Keep(
                outcome == EncounterOutcome.Lost ? hero.CurrentHp : Math.Max(1, hero.CurrentHp),
                hero.CurrentMana
            );
        }

        if (outcome == EncounterOutcome.Lost && Status == DungeonRunStatus.Active)
        {
            Status = DungeonRunStatus.Lost;
        }
        else if (
            outcome == EncounterOutcome.Won
            && encounter.Trigger == EncounterTrigger.Boss
            && Status == DungeonRunStatus.Active
            && encounter.Floor == CurrentFloor
        )
        {
            IsFloorBossDefeated = true;
            if (dungeon.Floors[CurrentFloor].IsFinalFloor)
            {
                Status = DungeonRunStatus.Won;
            }
        }

        encounter.Resolve(combatId, outcome, Turn);
    }

    private RunEncounter RequireEncounter(Guid encounterId) =>
        _encounters.SingleOrDefault(encounter => encounter.Id == encounterId)
        ?? throw new EncounterNotFoundException(Id, encounterId, "no such encounter in this run");

    /// <summary>
    /// Debug only: puts a new mob on a free floor tile of the sandbox (<see cref="SandboxDungeon"/>).
    /// It counts as an action, but the mobs do not play: they move when the hero does.
    /// </summary>
    public void SpawnMob(EnemyKind kind, Position at, GeneratedDungeon dungeon)
    {
        DungeonFloor floor = GetCurrentFloor(dungeon);

        if (!dungeon.IsSandbox)
        {
            throw new InvalidSpawnException(at, "mobs are only spawned by hand in the sandbox");
        }

        if (!floor.IsWalkable(at) || floor.GetRoomId(at) is not int roomId)
        {
            throw new InvalidSpawnException(at, "the tile is not the floor of a room");
        }

        if (at == HeroPosition || _mobs.Any(mob => mob.IsActive && mob.Position == at))
        {
            throw new InvalidSpawnException(at, "the tile is taken");
        }

        Guid id = Guid.NewGuid();
        _mobs.Add(RunMob.Spawn(new DungeonElement(id, ElementType.Enemy, at, roomId, kind)));
        Turn++;
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
    public bool OpenChest(Guid elementId, GeneratedDungeon dungeon, DateTimeOffset openedAt)
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
