using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Moq;

namespace Dungeon.Test.Features.DungeonRunUseCase.GetDungeonRunById;

public class GetDungeonRunByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ActiveRun_ListsItsLivingMobs()
    {
        // Arrange: one mob dead, one mage with an announced jump.
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow
        );
        run.Mobs[0].Kill(run.Turn - 1);
        RunMob announcer = run.Mobs[1];
        announcer.Announce(new Position(announcer.X + 1, announcer.Y));
        Mock<IDungeonRunRepository> repository = new();
        repository
            .Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        GetDungeonRunByIdQueryHandler handler = new(
            repository.Object,
            new GeneratingDungeonProvider()
        );

        // Act
        GetDungeonRunByIdResult result = await handler.Handle(
            new GetDungeonRunByIdQuery(run.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        result
            .Mobs.Select(mob => mob.Id)
            .Should()
            .Equal(run.Mobs.Skip(1).Select(mob => mob.ElementId));
        result
            .Mobs.Should()
            .OnlyContain(mob => new[] { "zombie", "mage", "goblin", "bat" }.Contains(mob.Kind));
        result.Mobs[0].X.Should().Be(announcer.X);
        result
            .Mobs[0]
            .NextTeleport.Should()
            .Be(new Application.Models.PositionResult(announcer.X + 1, announcer.Y));
        result.Mobs.Skip(1).Should().OnlyContain(mob => mob.NextTeleport == null && !mob.Aggro);
    }

    [Fact]
    public async Task Handle_MobKilledThisTurn_IsListedWhereItFell()
    {
        // Arrange: one mob killed on this turn, one on an earlier turn.
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow
        );
        RunMob earlier = run.Mobs[0];
        RunMob now = run.Mobs[1];
        earlier.Kill(run.Turn - 1);
        now.MoveTo(new Position(now.X + 1, now.Y));
        now.Kill(run.Turn);
        Mock<IDungeonRunRepository> repository = new();
        repository
            .Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        GetDungeonRunByIdQueryHandler handler = new(
            repository.Object,
            new GeneratingDungeonProvider()
        );

        // Act
        GetDungeonRunByIdResult result = await handler.Handle(
            new GetDungeonRunByIdQuery(run.Id),
            TestContext.Current.CancellationToken
        );

        // Assert: the front lets it slide to its last tile before it fades out.
        result
            .KilledMobs.Select(mob => (mob.Id, mob.X, mob.Y))
            .Should()
            .Equal((now.ElementId, now.X, now.Y));
        result.Mobs.Select(mob => mob.Id).Should().NotContain([earlier.ElementId, now.ElementId]);
    }
}
