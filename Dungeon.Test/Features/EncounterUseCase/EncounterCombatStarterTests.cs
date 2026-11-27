using Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatStarted;
using Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatUnavailable;
using Dungeon.Application.Ports;
using Dungeon.Application.Services;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using MediatR;
using Moq;
using Serilog.Core;

namespace Dungeon.Test.Features.EncounterUseCase;

public sealed class EncounterCombatStarterTests
{
    private static readonly Guid HeroId = Guid.NewGuid();
    private readonly Mock<IDungeonRunRepository> _repository = new();
    private readonly Mock<ICombatGateway> _combat = new();
    private readonly Mock<ISender> _sender = new();
    private readonly EncounterCombatStarter _starter;

    public EncounterCombatStarterTests()
    {
        _starter = new EncounterCombatStarter(
            _repository.Object,
            _combat.Object,
            _sender.Object,
            Logger.None
        );
    }

    [Fact]
    public async Task StartPending_PendingEncounter_OpensTheFightWithTheEncounterAsKey()
    {
        DungeonRun run = RunInContact();
        RunEncounter encounter = run.OpenEncounter!;
        Guid combatId = Guid.NewGuid();
        CombatEncounterRequest? sent = null;
        _combat
            .Setup(gateway =>
                gateway.CreateCombatAsync(
                    It.IsAny<CombatEncounterRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<CombatEncounterRequest, CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(combatId);

        bool started = await _starter.StartPendingAsync(
            run.Id,
            TestContext.Current.CancellationToken
        );

        started.Should().BeTrue();
        sent!.EncounterId.Should().Be(encounter.Id);
        sent.Floor.Should().Be(run.CurrentFloor + 1, "Combat counts floors from 1");
        sent.Heroes.Should().ContainSingle(hero => hero.HeroId == HeroId);
        sent.Monsters.Select(monster => monster.ElementId).Should().Equal(encounter.MobElementIds);
        _sender.Verify(sender =>
            sender.Send(
                new MarkEncounterCombatStartedCommand(run.Id, encounter.Id, combatId),
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task StartPending_CombatUnavailable_MarksTheEncounterFailedNeverWon()
    {
        DungeonRun run = RunInContact();
        _combat
            .Setup(gateway =>
                gateway.CreateCombatAsync(
                    It.IsAny<CombatEncounterRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new CombatUnavailableException("down"));

        bool started = await _starter.StartPendingAsync(
            run.Id,
            TestContext.Current.CancellationToken
        );

        started.Should().BeFalse();
        _sender.Verify(sender =>
            sender.Send(
                new MarkEncounterCombatUnavailableCommand(run.Id, run.OpenEncounter!.Id),
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task StartPending_NoEncounter_DoesNotCallCombat()
    {
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["#####", "#H..#", "#####"],
            new RoomBounds(1, 1, 3, 1)
        );
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow,
            [HeroId]
        );
        _repository
            .Setup(repository => repository.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        bool started = await _starter.StartPendingAsync(
            run.Id,
            TestContext.Current.CancellationToken
        );

        started.Should().BeTrue();
        _combat.VerifyNoOtherCalls();
    }

    private DungeonRun RunInContact()
    {
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["#######", "#Hz...#", "#######"],
            new RoomBounds(1, 1, 5, 1)
        );
        DungeonRun run = DungeonRun.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dungeon,
            DateTimeOffset.UtcNow,
            [HeroId]
        );
        run.MoveHero(
            Direction.East,
            dungeon,
            MobRules.Default,
            new EncounterRules(EncounterMode.Combat, 1, 6)
        );
        _repository
            .Setup(repository => repository.GetByIdAsync(run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        return run;
    }
}
