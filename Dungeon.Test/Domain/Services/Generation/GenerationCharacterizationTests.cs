using System.Text;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Services.Generation;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Services.Generation;

/// <summary>
/// Characterization of everything the generator and the debug sandbox decide, for many seeds
/// and every easter-egg odds. A refactoring of <c>FloorBuilder</c>, <c>SandboxDungeon</c> or
/// the entities they build must leave these fingerprints alone: the same seed gives the same
/// dungeon, to the last tile, element and decoration. If one changes on purpose, bump
/// <see cref="DungeonGenerator.CurrentVersion"/> and update the value.
/// </summary>
public class GenerationCharacterizationTests
{
    private const string GeneratedDungeonsFingerprint = "3ce74f2759406ff7";
    private const string SandboxFingerprint = "b057fa3d8855126b";

    [Fact]
    public void Generate_ManySeedsAndEveryOdds_MatchesTheRecordedFingerprint()
    {
        // Act: odds of 0 (no easter egg), 1 (one on every floor) and the default.
        ulong hash = Fnv.Offset;
        foreach (int oneIn in new[] { 0, 1, EasterEggs.DefaultOneIn })
        {
            DungeonGenerator generator = new(easterEggOneIn: oneIn);
            foreach (Seed seed in DungeonTestData.SampleSeeds(60, origin: 1789))
            {
                hash = Fnv.Append(
                    hash,
                    FullSnapshot(generator.Generate(seed, DungeonSettings.Default))
                );
            }
        }

        // Assert
        hash.ToString("x16").Should().Be(GeneratedDungeonsFingerprint);
    }

    [Fact]
    public void Generate_OtherSettings_MatchesTheRecordedFingerprintToo()
    {
        // Act: a three-floor and a one-floor dungeon go through the final-floor branches.
        ulong hash = Fnv.Offset;
        foreach (DungeonSettings settings in new DungeonSettings[] { new(40, 3), new(24, 1) })
        {
            DungeonGenerator generator = new(easterEggOneIn: 1);
            foreach (Seed seed in DungeonTestData.SampleSeeds(20, origin: 4242))
            {
                hash = Fnv.Append(hash, FullSnapshot(generator.Generate(seed, settings)));
            }
        }

        // Assert
        hash.ToString("x16").Should().Be(OtherSettingsFingerprint);
    }

    private const string OtherSettingsFingerprint = "1ed02097e3778b96";

    [Fact]
    public void Sandbox_MatchesTheRecordedFingerprint()
    {
        // Act
        GeneratedDungeon sandbox = new DungeonGenerator(sandboxEnabled: true).Generate(
            SandboxDungeon.Seed,
            DungeonSettings.Default
        );

        // Assert
        Fnv.Append(Fnv.Offset, FullSnapshot(sandbox))
            .ToString("x16")
            .Should()
            .Be(SandboxFingerprint);
    }

    /// <summary>
    /// Unlike <see cref="DungeonTestData.Snapshot"/>, which pins the golden master, this one
    /// also holds what the easter eggs added: decorations and the easter egg of a room.
    /// </summary>
    private static string FullSnapshot(GeneratedDungeon dungeon)
    {
        StringBuilder builder = new();
        builder.Append(
            $"{dungeon.Seed}|v{dungeon.GeneratorVersion}|{dungeon.Settings.RoomCount}/{dungeon.Settings.FloorCount}|sandbox {dungeon.IsSandbox}"
        );

        foreach (DungeonFloor floor in dungeon.Floors)
        {
            builder.Append(
                $"\nfloor {floor.Index} final {floor.IsFinalFloor} {floor.Width}x{floor.Height} entrance {floor.Entrance}\n"
            );
            for (int y = 0; y < floor.Height; y++)
            {
                for (int x = 0; x < floor.Width; x++)
                {
                    builder.Append((char)('a' + (int)floor.GetCell(new Position(x, y))));
                }

                builder.Append('\n');
            }

            foreach (Room room in floor.Rooms)
            {
                builder.Append(
                    $"room {room.Id} {room.Type} grid {room.GridCell} {room.Interior} centre {room.Center} d{room.Depth} [{string.Join(',', room.ConnectedRoomIds)}] egg {room.EasterEgg}\n"
                );
            }

            foreach (DungeonElement element in floor.Elements)
            {
                builder.Append(
                    $"element {element.Id} {element.Type} {element.Position} r{element.RoomId} k{element.Kind} d{element.Decoration}\n"
                );
            }
        }

        return builder.ToString();
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
