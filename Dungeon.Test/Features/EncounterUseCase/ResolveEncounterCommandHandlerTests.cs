using Dungeon.Application.Features.EncounterUseCase.ResolveEncounter;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Moq;
using Serilog.Core;

namespace Dungeon.Test.Features.EncounterUseCase;

/// <summary>What Combat reports becomes the state of the run; the same report twice changes nothing.</summary>
public sealed class ResolveEncounterCommandHandlerTests
{
    private static readonly Guid HeroId = Guid.Parse("00000000-0000-0000-0000-00000000aaaa");
    private readonly Mock<IDungeonRunRepository> _repository = new();
    private readonly Mock<IDungeonProvider> _provider = new();
    private readonly ResolveEncounterCommandHandler _handler;
    private readonly GeneratedDungeon _dungeon = DungeonTestData.Arena(
        ["#######", "#Hz...#", "#######"],
        new RoomBounds(1, 1, 5, 1)
    );

    public ResolveEncounterCommandHandlerTests()
    {
        _provider
            .Setup(provider =>
                provider.Get(It.IsAny<Seed>(), It.IsAny<DungeonSettings>(), It.IsAny<int>())
            )
            .Returns(_dungeon);
        _handler = new ResolveEncounterCommandHandler(
            _repository.Object,
            _provider.Object,
            Logger.None
        );
    }

    [Fact]
    public async Task Handle_Won_KeepsTheRunActiveAndKillsThePack()
    {
        DungeonRun run = RunInContact();
        RunEncounter encounter = run.OpenEncounter!;

        string status = await _handler.Handle(
            Command(run, encounter, Guid.NewGuid(), EncounterOutcome.Won),
            TestContext.Current.CancellationToken
        );

        status.Should().Be("active");
        run.Mobs.Should().OnlyContain(mob => !mob.IsActive);
        run.OpenEncounter.Should().BeNull();
        run.Participants.Single().CurrentHp.Should().Be(80);
    }

    [Fact]
    public async Task Handle_Lost_LosesTheRun()
    {
        DungeonRun run = RunInContact();

        string status = await _handler.Handle(
            Command(run, run.OpenEncounter!, Guid.NewGuid(), EncounterOutcome.Lost),
            TestContext.Current.CancellationToken
        );

        status.Should().Be("lost");
        run.Status.Should().Be(DungeonRunStatus.Lost);
    }

    [Theory]
    [InlineData(EncounterOutcome.Won, "active")]
    [InlineData(EncounterOutcome.Lost, "lost")]
    public async Task Handle_SameCombatReportedAgain_ReturnsTheSameStatusAndChangesNothing(
        EncounterOutcome outcome,
        string expectedStatus
    )
    {
        DungeonRun run = RunInContact();
        ResolveEncounterCommand command = Command(run, run.OpenEncounter!, Guid.NewGuid(), outcome);
        await _handler.Handle(command, TestContext.Current.CancellationToken);
        int turn = run.Turn;

        string replayed = await _handler.Handle(command, TestContext.Current.CancellationToken);

        replayed.Should().Be(expectedStatus);
        run.Turn.Should().Be(turn);
    }

    [Fact]
    public async Task Handle_UnknownRun_IsNotFound()
    {
        Guid runId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DungeonRun?)null);

        Func<Task> act = () =>
            _handler.Handle(
                new ResolveEncounterCommand(
                    runId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    EncounterOutcome.Won,
                    [],
                    []
                ),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static ResolveEncounterCommand Command(
        DungeonRun run,
        RunEncounter encounter,
        Guid combatId,
        EncounterOutcome outcome
    ) =>
        new(
            run.Id,
            encounter.Id,
            combatId,
            outcome,
            [new HeroFinalState(HeroId, 80, 10, outcome == EncounterOutcome.Lost)],
            outcome == EncounterOutcome.Won ? [.. encounter.MobElementIds] : []
        );

    private DungeonRun RunInContact()
    {
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _dungeon,
            DateTimeOffset.UtcNow,
            [HeroId]
        );
        run.MoveHero(
            Direction.East,
            _dungeon,
            MobRules.Default,
            new EncounterRules(EncounterMode.Combat, 1, 6)
        );
        _repository
            .Setup(repository => repository.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        return run;
    }
}
