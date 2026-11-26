using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunHeroes;
using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Moq;

namespace Dungeon.Test.Features.DungeonRunUseCase.GetDungeonRunHeroes;

public class GetDungeonRunHeroesQueryHandlerTests
{
    private readonly Mock<IDungeonRunRepository> _repositoryMock = new();
    private readonly Mock<IHeroAppearanceClient> _appearanceClientMock = new();
    private readonly GetDungeonRunHeroesQueryHandler _handler;

    public GetDungeonRunHeroesQueryHandlerTests()
    {
        _handler = new GetDungeonRunHeroesQueryHandler(
            _repositoryMock.Object,
            _appearanceClientMock.Object
        );
    }

    [Fact]
    public async Task Handle_HeroWithAChosenAppearance_ReturnsThatAppearance()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        DungeonRun run = StoredRun(heroId);
        HeroAppearance chosen = new(4, "dap1", 7);
        _appearanceClientMock
            .Setup(client => client.GetAppearanceAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chosen);

        // Act
        GetDungeonRunHeroesResult result = await _handler.Handle(
            new GetDungeonRunHeroesQuery(run.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.RunId.Should().Be(run.Id);
        result.Heroes.Should().Equal(new RunHeroResult(heroId, chosen, false));
    }

    [Fact]
    public async Task Handle_PlayerCannotDescribeTheHero_FallsBackToTheDefaultAppearance()
    {
        // Arrange: unknown hero, Player unreachable or malformed answer all end up as null.
        Guid heroId = Guid.NewGuid();
        DungeonRun run = StoredRun(heroId);
        _appearanceClientMock
            .Setup(client => client.GetAppearanceAsync(heroId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeroAppearance?)null);

        // Act
        GetDungeonRunHeroesResult result = await _handler.Handle(
            new GetDungeonRunHeroesQuery(run.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Heroes.Should().Equal(new RunHeroResult(heroId, HeroAppearance.Default, true));
    }

    [Fact]
    public async Task Handle_Party_KeepsTheOrderOfTheRunAndFallsBackHeroByHero()
    {
        // Arrange
        Guid described = Guid.NewGuid();
        Guid unknown = Guid.NewGuid();
        DungeonRun run = StoredRun(unknown, described);
        HeroAppearance bald = new(2, "bald", null);
        _appearanceClientMock
            .Setup(client => client.GetAppearanceAsync(described, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bald);
        _appearanceClientMock
            .Setup(client => client.GetAppearanceAsync(unknown, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HeroAppearance?)null);

        // Act
        GetDungeonRunHeroesResult result = await _handler.Handle(
            new GetDungeonRunHeroesQuery(run.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        result
            .Heroes.Should()
            .Equal(
                new RunHeroResult(unknown, HeroAppearance.Default, true),
                new RunHeroResult(described, bald, false)
            );
    }

    [Fact]
    public async Task Handle_RunWithoutHeroes_ReturnsNoHeroAndDoesNotCallPlayer()
    {
        // Arrange: a run created before the heroes were kept, or without Player.
        DungeonRun run = StoredRun();

        // Act
        GetDungeonRunHeroesResult result = await _handler.Handle(
            new GetDungeonRunHeroesQuery(run.Id),
            TestContext.Current.CancellationToken
        );

        // Assert
        result.Heroes.Should().BeEmpty();
        _appearanceClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UnknownRun_ThrowsNotFound()
    {
        // Arrange
        Guid runId = Guid.NewGuid();
        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DungeonRun?)null);

        // Act
        Func<Task> act = () =>
            _handler.Handle(
                new GetDungeonRunHeroesQuery(runId),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private DungeonRun StoredRun(params Guid[] heroIds)
    {
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DungeonTestData.Generate(DungeonTestData.ReferenceSeed),
            DateTimeOffset.UtcNow,
            heroIds
        );
        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        return run;
    }
}
