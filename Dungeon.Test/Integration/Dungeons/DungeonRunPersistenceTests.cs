using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.ValueObjects;
using Dungeon.Infrastructure.Persistence;
using Dungeon.Infrastructure.Persistence.Repositories;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Dungeon.Test.Integration.Dungeons;

/// <summary>The mobs of a run survive a round trip through PostgreSQL and its migrations.</summary>
public sealed class DungeonRunPersistenceTests : IAsyncLifetime
{
    private TestDatabase? database;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"dungeon_test_run_mobs_{Guid.NewGuid():N}");
    }

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private DungeonDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<DungeonDbContext>()
                .UseNpgsql(database!.ConnectionString)
                .Options
        );

    private static DungeonRun NewRun() =>
        DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DungeonTestData.Generate(DungeonTestData.ReferenceSeed),
            DateTimeOffset.UtcNow
        );

    [Fact]
    public async Task SaveThenLoad_KeepsTheMobsAndTheDiscoveredRooms()
    {
        // Arrange
        DungeonRun run = NewRun();
        await using (DungeonDbContext context = NewContext())
        {
            await new DungeonRunRepository(context).AddAsync(
                run,
                TestContext.Current.CancellationToken
            );
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using DungeonDbContext reading = NewContext();
        DungeonRun? loaded = await new DungeonRunRepository(reading).GetByIdAsync(
            run.Id,
            TestContext.Current.CancellationToken
        );

        // Assert
        loaded.Should().NotBeNull();
        loaded!
            .Mobs.Select(mob =>
                (mob.ElementId, mob.Kind, mob.Position, mob.HomeRoomId, mob.IsActive)
            )
            .Should()
            .Equal(
                run.Mobs.Select(mob =>
                    (mob.ElementId, mob.Kind, mob.Position, mob.HomeRoomId, mob.IsActive)
                )
            );
        loaded.DiscoveredRoomIds.Should().Equal(run.DiscoveredRoomIds);
    }

    [Fact]
    public async Task Load_RunWithoutMobRows_HasNoMobs()
    {
        // Arrange: a run saved before mobs were kept in the run.
        DungeonRun run = NewRun();
        await using (DungeonDbContext context = NewContext())
        {
            await new DungeonRunRepository(context).AddAsync(
                run,
                TestContext.Current.CancellationToken
            );
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"DungeonRunMobs\"",
                TestContext.Current.CancellationToken
            );
        }

        // Act
        await using DungeonDbContext reading = NewContext();
        DungeonRun? loaded = await new DungeonRunRepository(reading).GetByIdAsync(
            run.Id,
            TestContext.Current.CancellationToken
        );

        // Assert: it plays its current floor without mobs, it does not fail.
        loaded!.Mobs.Should().BeEmpty();
    }

    [Fact]
    public async Task TwoMovesFromTheSameTurn_TheSecondFailsAndWritesNoMob()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow
        );
        await using (DungeonDbContext context = NewContext())
        {
            await new DungeonRunRepository(context).AddAsync(
                run,
                TestContext.Current.CancellationToken
            );
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using DungeonDbContext first = NewContext();
        await using DungeonDbContext second = NewContext();
        DungeonRun firstCopy = (
            await new DungeonRunRepository(first).GetByIdAsync(
                run.Id,
                TestContext.Current.CancellationToken
            )
        )!;
        DungeonRun secondCopy = (
            await new DungeonRunRepository(second).GetByIdAsync(
                run.Id,
                TestContext.Current.CancellationToken
            )
        )!;
        Direction step = FirstOpenDirection(dungeon.Floors[0], run.HeroPosition);
        firstCopy.MoveHero(step, dungeon, AllHunters());
        secondCopy.MoveHero(step, dungeon, AllHunters());

        // Act
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);
        Func<Task> act = () => second.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert: Turn is the concurrency token of the whole run, mobs included.
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using DungeonDbContext reading = NewContext();
        DungeonRun stored = (
            await new DungeonRunRepository(reading).GetByIdAsync(
                run.Id,
                TestContext.Current.CancellationToken
            )
        )!;
        stored.Turn.Should().Be(1);
        stored
            .Mobs.Select(mob => (mob.Position, mob.IsAggro))
            .Should()
            .Equal(firstCopy.Mobs.Select(mob => (mob.Position, mob.IsAggro)));
    }

    private static MobRules AllHunters()
    {
        MobBehaviour hunter = new("1", 1000, MobStep.Orthogonal, MobIdle.None);
        return new MobRules(2, Enum.GetValues<EnemyKind>().ToDictionary(kind => kind, _ => hunter));
    }

    private static Direction FirstOpenDirection(DungeonFloor floor, Position from) =>
        Enum.GetValues<Direction>().First(direction => floor.IsWalkable(from.Step(direction)));
}
