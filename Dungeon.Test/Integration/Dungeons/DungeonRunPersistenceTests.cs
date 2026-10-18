using Dungeon.Domain.Entities;
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
}
