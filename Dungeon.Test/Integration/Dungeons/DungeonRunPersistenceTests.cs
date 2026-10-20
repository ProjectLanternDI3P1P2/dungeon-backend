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

    [Fact]
    public async Task Load_AfterGoingDown_GivesTheMobsInElementOrder()
    {
        // Arrange: on the way down, the rows of the next floor are partly updated (reused
        // element ids), partly inserted, and PostgreSQL returns them in its own order.
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);

        // Act
        (DungeonRun stored, _) = await GoDownThenLoad(dungeon);

        // Assert: the mobs act in this order, so a replay depends on it.
        stored.CurrentFloor.Should().Be(1);
        stored
            .Mobs.Select(mob => mob.ElementId)
            .Should()
            .Equal(
                dungeon
                    .Floors[1]
                    .Elements.Where(element => element.Type == ElementType.Enemy)
                    .Select(element => element.Id)
            );
    }

    [Fact]
    public async Task Load_AfterGoingDown_ReusedElementIdsSpawnAfresh()
    {
        // Arrange: element ids restart on every floor, so the rows of the floor above are
        // overwritten under the same key (DungeonRunId, ElementId).
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonElement[] spawns =
        [
            .. dungeon.Floors[1].Elements.Where(element => element.Type == ElementType.Enemy),
        ];

        // Act
        (DungeonRun stored, IReadOnlyList<RunMob> above) = await GoDownThenLoad(dungeon);

        // Assert: the reused rows carried state from the floor above, and none of it is left.
        above
            .Should()
            .Contain(mob => spawns.Any(spawn => spawn.Id == mob.ElementId) && mob.Tick > 0);
        stored
            .Mobs.Select(mob =>
                (
                    mob.ElementId,
                    mob.Kind,
                    mob.Position,
                    mob.HomeRoomId,
                    mob.IsActive,
                    mob.IsAggro,
                    mob.Tick,
                    mob.Teleport,
                    mob.Door,
                    mob.KilledOnTurn
                )
            )
            .Should()
            .Equal(
                spawns.Select(spawn =>
                    (
                        spawn.Id,
                        spawn.Kind!.Value,
                        spawn.Position,
                        spawn.RoomId,
                        true,
                        false,
                        0,
                        (Position?)null,
                        (Position?)null,
                        (int?)null
                    )
                )
            );
    }

    /// <summary>
    /// Plays the first floor down to the gate in one context, saves, and loads the run again.
    /// </summary>
    /// <returns>The run as stored, and its mobs just before the party went down.</returns>
    private async Task<(DungeonRun Stored, IReadOnlyList<RunMob> Above)> GoDownThenLoad(
        GeneratedDungeon dungeon
    )
    {
        CancellationToken cancellation = TestContext.Current.CancellationToken;
        DungeonFloor floor = dungeon.Floors[0];
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow
        );
        await using (DungeonDbContext context = NewContext())
        {
            await new DungeonRunRepository(context).AddAsync(run, cancellation);
            await context.SaveChangesAsync(cancellation);
        }

        IReadOnlyList<RunMob> above;
        await using (DungeonDbContext context = NewContext())
        {
            DungeonRun loaded = (
                await new DungeonRunRepository(context).GetByIdAsync(run.Id, cancellation)
            )!;
            Position boss = floor
                .Elements.Single(element => element.Type == ElementType.Boss)
                .Position;
            DungeonTestData.WalkTo(loaded, dungeon, boss);
            loaded.DefeatFloorBoss(dungeon);
            Position gate = DungeonTestData.PositionsOf(floor, CellType.Gate).Single();
            DungeonTestData.WalkTo(loaded, dungeon, gate.Step(Direction.South));
            above = loaded.Mobs;
            loaded.MoveHero(Direction.North, dungeon, MobRules.Default);
            await context.SaveChangesAsync(cancellation);
        }

        await using DungeonDbContext reading = NewContext();
        DungeonRun stored = (
            await new DungeonRunRepository(reading).GetByIdAsync(run.Id, cancellation)
        )!;
        return (stored, above);
    }

    private static MobRules AllHunters()
    {
        MobBehaviour hunter = new("1", 1000, MobStep.Orthogonal, MobIdle.None);
        return new MobRules(2, Enum.GetValues<EnemyKind>().ToDictionary(kind => kind, _ => hunter));
    }

    private static Direction FirstOpenDirection(DungeonFloor floor, Position from) =>
        Enum.GetValues<Direction>().First(direction => floor.IsWalkable(from.Step(direction)));
}
