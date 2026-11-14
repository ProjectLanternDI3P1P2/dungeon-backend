using System.Text;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Services.Randomness;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

/// <summary>
/// Characterization of a whole turn of the mobs (<c>MobTurn.Play</c>): long, deterministic
/// games, with every state of every mob (position, aggro, tick, announced jump, door held,
/// death) recorded turn after turn. A refactoring of the turn must leave the fingerprint
/// alone: the same game gives the same mobs, to the last step.
/// </summary>
public class MobTurnCharacterizationTests
{
    private const string ArenaGamesFingerprint = "6edc92f994f7f329";
    private const string GeneratedGamesFingerprint = "abe5fcf4b72afc61";

    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly Direction[] Directions = Enum.GetValues<Direction>();

    [Fact]
    public void Play_EveryKindOfMobInAnArena_MatchesTheRecordedFingerprint()
    {
        // Arrange: three rooms, two doors, every kind of mob. (Not the debug sandbox: the mobs
        // spawned there get random ids, and their order, hence their game, differs every run.)
        GeneratedDungeon arena = Arena();
        ulong hash = Fnv.Offset;
        Seen seen = new();

        // Act
        foreach (ulong origin in new ulong[] { 1, 2, 3, 4, 5, 6, 7, 8 })
        {
            DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), arena, Now);
            hash = Fnv.Append(hash, Play(run, arena, 300, origin, seen, hunt: origin % 2 == 0));
        }

        // Assert: the games went through every branch of the turn, or the fingerprint says little.
        seen.Should()
            .BeEquivalentTo(
                new Seen
                {
                    Aggro = true,
                    Killed = true,
                    Jumped = true,
                    HeldADoor = true,
                    WanderedOrChased = true,
                }
            );
        hash.ToString("x16").Should().Be(ArenaGamesFingerprint);
    }

    [Fact]
    public void Play_GeneratedDungeons_MatchesTheRecordedFingerprint()
    {
        // Arrange: real floors, the hero walking toward the nearest living mob half of the time.
        ulong hash = Fnv.Offset;
        Seen seen = new();

        // Act
        foreach (Seed seed in DungeonTestData.SampleSeeds(10, origin: 777))
        {
            GeneratedDungeon dungeon = DungeonTestData.Generate(seed);
            DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);
            hash = Fnv.Append(hash, Play(run, dungeon, 400, seed.Value, seen, hunt: true));
        }

        // Assert
        seen.Aggro.Should().BeTrue();
        seen.Killed.Should().BeTrue();
        hash.ToString("x16").Should().Be(GeneratedGamesFingerprint);
    }

    /// <summary>
    /// The start room (x 1-13) opens through the door (14, 3) onto a long room (x 15-38, y 1-6),
    /// whose door (26, 7) gives onto a third room (y 8-13): two goblins, two mages, two bats
    /// and four zombies stand about.
    /// </summary>
    private static GeneratedDungeon Arena()
    {
        char[][] rows = Enumerable
            .Range(0, 15)
            .Select(_ => new string('#', 40).ToCharArray())
            .ToArray();
        void Carve(int x, int y, int width, int height)
        {
            for (int row = y; row < y + height; row++)
            {
                for (int column = x; column < x + width; column++)
                {
                    rows[row][column] = '.';
                }
            }
        }

        Carve(1, 1, 13, 5);
        Carve(15, 1, 24, 6);
        Carve(15, 8, 24, 6);
        rows[3][14] = '+';
        rows[7][26] = '+';
        rows[1][1] = 'H';
        foreach (
            (char mob, int x, int y) in new[]
            {
                ('z', 9, 2),
                ('b', 11, 4),
                ('g', 20, 3),
                ('m', 30, 4),
                ('z', 35, 2),
                ('g', 20, 10),
                ('m', 33, 11),
                ('b', 25, 12),
                ('z', 18, 9),
                ('z', 36, 12),
            }
        )
        {
            rows[y][x] = mob;
        }

        return DungeonTestData.Arena(
            [.. rows.Select(row => new string(row))],
            new RoomBounds(1, 1, 13, 5),
            new RoomBounds(15, 1, 24, 6),
            new RoomBounds(15, 8, 24, 6)
        );
    }

    /// <summary>Plays the turns and records everything the mobs did, as a hash.</summary>
    private static string Play(
        DungeonRun run,
        GeneratedDungeon dungeon,
        int turns,
        ulong origin,
        Seen seen,
        bool hunt = false
    )
    {
        ulong state = origin * 2654435761 + 12345;
        StringBuilder log = new();
        Dictionary<Guid, Position> before = [];

        for (int turn = 0; turn < turns; turn++)
        {
            Direction direction = Choose(run, dungeon, ref state, hunt);
            try
            {
                run.MoveHero(direction, dungeon, MobRules.Default);
            }
            catch (DomainException)
            {
                // A wall, a mob, a gate that stays shut: the turn does not pass.
            }

            log.Append(
                $"t{run.Turn} f{run.CurrentFloor} h{run.HeroPosition} rooms[{string.Join(',', run.DiscoveredRoomIds)}]\n"
            );
            foreach (RunMob mob in run.Mobs)
            {
                seen.Note(mob, before);
                log.Append(
                    $"  {mob.ElementId} {mob.Kind} {mob.Position} home{mob.HomeRoomId} active{mob.IsActive} killed{mob.KilledOnTurn} aggro{mob.IsAggro} tick{mob.Tick} jump{mob.Teleport} door{mob.Door}\n"
                );
            }
        }

        return log.ToString();
    }

    private static Direction Choose(
        DungeonRun run,
        GeneratedDungeon dungeon,
        ref ulong state,
        bool hunt
    )
    {
        ulong roll = SplitMix64.Next(ref state);
        if (hunt && roll % 2 == 0)
        {
            DungeonFloor floor = dungeon.Floors[run.CurrentFloor];
            Position? target = run
                .Mobs.Where(mob => mob.IsActive)
                .Select(mob => mob.Position)
                .OrderBy(position => position.DistanceTo(run.HeroPosition))
                .ThenBy(position => position.Y)
                .ThenBy(position => position.X)
                .Cast<Position?>()
                .FirstOrDefault();
            if (target is Position found)
            {
                List<Direction>? path = DungeonTestData.FindPath(floor, run.HeroPosition, found);
                if (path is { Count: > 0 })
                {
                    return path[0];
                }
            }
        }

        return Directions[(int)((roll >> 8) % (ulong)Directions.Length)];
    }

    /// <summary>What the games went through: each flag must end up true.</summary>
    public sealed class Seen
    {
        public bool Aggro { get; set; }

        public bool Killed { get; set; }

        public bool Jumped { get; set; }

        public bool HeldADoor { get; set; }

        public bool WanderedOrChased { get; set; }

        public void Note(RunMob mob, Dictionary<Guid, Position> before)
        {
            Aggro |= mob.IsAggro;
            Killed |= mob.KilledOnTurn is not null;
            Jumped |= mob.Teleport is not null;
            HeldADoor |= mob.Door is not null;
            WanderedOrChased |=
                before.TryGetValue(mob.ElementId, out Position previous)
                && previous != mob.Position;
            before[mob.ElementId] = mob.Position;
        }
    }

    /// <summary>64-bit FNV-1a, chained from one text to the next.</summary>
    private static class Fnv
    {
        public const ulong Offset = 14695981039346656037;

        public static ulong Append(ulong hash, string text)
        {
            foreach (char character in text)
            {
                hash = unchecked((hash ^ character) * 1099511628211);
            }

            return hash;
        }
    }
}
