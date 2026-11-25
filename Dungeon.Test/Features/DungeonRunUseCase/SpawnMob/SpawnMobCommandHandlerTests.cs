using Dungeon.Application.Features.DungeonRunUseCase.SpawnMob;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Repositories;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace Dungeon.Test.Features.DungeonRunUseCase.SpawnMob;

public class SpawnMobCommandHandlerTests
{
    private readonly Mock<IDungeonRunRepository> _dungeonRunRepositoryMock = new();
    private readonly SpawnMobCommandHandler _handler;
    private readonly DungeonRun _run;

    public SpawnMobCommandHandlerTests()
    {
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        _run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, DateTimeOffset.UtcNow);
        _dungeonRunRepositoryMock
            .Setup(repository => repository.GetByIdAsync(_run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_run);

        _handler = new SpawnMobCommandHandler(
            _dungeonRunRepositoryMock.Object,
            new GeneratingDungeonProvider()
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("dragon")]
    public async Task Handle_UnknownKind_IsRefusedAsInvalidAndSpawnsNothing(string kind)
    {
        // Arrange: a command that skipped the validator. The default of a failed parse is a
        // zombie: none must appear.
        int mobs = _run.Mobs.Count;
        int turn = _run.Turn;

        // Act
        Func<Task> act = async () =>
            await _handler.Handle(
                new SpawnMobCommand(_run.Id, kind, 1, 1),
                TestContext.Current.CancellationToken
            );

        // Assert: the same refusal as the pipeline, answered 422.
        ValidationException refusal = (await act.Should().ThrowAsync<ValidationException>()).Which;
        refusal.Errors.Should().ContainSingle();
        refusal.Errors.Single().PropertyName.Should().Be(nameof(SpawnMobCommand.Kind));
        refusal.Errors.Single().ErrorMessage.Should().Be(SpawnMobValidator.KindMessage);
        _run.Mobs.Should().HaveCount(mobs);
        _run.Turn.Should().Be(turn);
    }

    [Theory]
    [InlineData("zombie")]
    [InlineData("MAGE")]
    public async Task Handle_KnownKindOutsideTheSandbox_ReachesTheDomainRuleAndIsRefused(
        string kind
    )
    {
        // Act
        Func<Task> act = async () =>
            await _handler.Handle(
                new SpawnMobCommand(_run.Id, kind, 1, 1),
                TestContext.Current.CancellationToken
            );

        // Assert: parsed, then refused by the domain: mobs are only spawned by hand in the sandbox.
        await act.Should().ThrowAsync<InvalidSpawnException>();
    }
}
