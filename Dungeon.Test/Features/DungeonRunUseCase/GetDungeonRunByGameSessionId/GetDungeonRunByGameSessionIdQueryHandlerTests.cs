using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Moq;

namespace Dungeon.Test.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;

public class GetDungeonRunByGameSessionIdQueryHandlerTests
{
    private readonly Mock<IDungeonRunRepository> _dungeonRunRepositoryMock = new();
    private readonly GetDungeonRunByGameSessionIdQueryHandler _handler;

    public GetDungeonRunByGameSessionIdQueryHandlerTests()
    {
        _handler = new GetDungeonRunByGameSessionIdQueryHandler(
            _dungeonRunRepositoryMock.Object,
            new GeneratingDungeonProvider()
        );
    }

    [Fact]
    public async Task Handle_SessionWithARun_ReturnsThatRun()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow
        );
        _dungeonRunRepositoryMock
            .Setup(repository =>
                repository.FindByGameSessionIdAsync(
                    run.GameSessionId,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(run);

        // Act
        GetDungeonRunByIdResult result = await _handler.Handle(
            new GetDungeonRunByGameSessionIdQuery(run.GameSessionId),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Id.Should().Be(run.Id);
        result.GameSessionId.Should().Be(run.GameSessionId);
        result.Seed.Should().Be(DungeonTestData.ReferenceSeed.ToString());
        result.Status.Should().Be("active");
        result.Hero.X.Should().Be(dungeon.Floors[0].Entrance.X);
        result.Hero.Y.Should().Be(dungeon.Floors[0].Entrance.Y);
    }

    [Fact]
    public async Task Handle_SessionWithoutARun_ThrowsNotFound()
    {
        // Act
        Func<Task> act = async () =>
            await _handler.Handle(
                new GetDungeonRunByGameSessionIdQuery(Guid.NewGuid()),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
