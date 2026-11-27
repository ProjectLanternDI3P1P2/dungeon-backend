using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Features.EncounterUseCase.ResolveEncounter;
using Dungeon.Contracts.V1;
using Dungeon.Domain.Enums;
using Dungeon.Presentation.Grpc.Services;
using Dungeon.Presentation.Hubs;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Serilog.Core;
using DomainHeroFinalState = Dungeon.Domain.ValueObjects.HeroFinalState;

namespace Dungeon.Test.Presentation.Grpc;

public class DungeonCombatGrpcServiceTests
{
    private static readonly Guid CommandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CombatId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RunId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EncounterId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid HeroId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid MobId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IDungeonClient> _groupMock = new();
    private readonly GetDungeonRunByIdResult _snapshot = new() { Id = RunId, Turn = 7 };
    private readonly DungeonCombatGrpcService _service;
    private ResolveEncounterCommand? _sent;

    public DungeonCombatGrpcServiceTests()
    {
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<ResolveEncounterCommand>(), It.IsAny<CancellationToken>())
            )
            .Callback<IRequest<string>, CancellationToken>(
                (command, _) => _sent = (ResolveEncounterCommand)command
            )
            .ReturnsAsync("active");
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<GetDungeonRunByIdQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(_snapshot);

        var clientsMock = new Mock<IHubClients<IDungeonClient>>();
        clientsMock
            .Setup(clients => clients.Group(DungeonHub.GroupName(RunId)))
            .Returns(_groupMock.Object);
        var hubContextMock = new Mock<IHubContext<DungeonHub, IDungeonClient>>();
        hubContextMock.SetupGet(context => context.Clients).Returns(clientsMock.Object);

        _service = new DungeonCombatGrpcService(
            _mediatorMock.Object,
            hubContextMock.Object,
            Logger.None
        );
    }

    [Theory]
    [InlineData(CombatOutcome.Won, EncounterOutcome.Won)]
    [InlineData(CombatOutcome.Lost, EncounterOutcome.Lost)]
    [InlineData(CombatOutcome.Fled, EncounterOutcome.Fled)]
    public async Task ReportCombatResult_ValidOutcome_ResolvesTheEncounterWithTheReportedState(
        CombatOutcome reported,
        EncounterOutcome expected
    )
    {
        // Arrange
        ReportCombatResultRequest request = Request(reported);

        // Act
        await ReportAsync(request);

        // Assert
        _sent.Should().NotBeNull();
        _sent!.RunId.Should().Be(RunId);
        _sent.EncounterId.Should().Be(EncounterId);
        _sent.CombatId.Should().Be(CombatId);
        _sent.Outcome.Should().Be(expected);
        _sent.Heroes.Should().Equal(new DomainHeroFinalState(HeroId, 42, 7, true));
        _sent.DefeatedElementIds.Should().Equal(MobId);
    }

    [Fact]
    public async Task ReportCombatResult_ReturnsTheRunStatusOfTheResolution()
    {
        // Arrange
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<ResolveEncounterCommand>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync("lost");

        // Act
        ReportCombatResultResponse response = await ReportAsync(Request(CombatOutcome.Lost));

        // Assert
        response.RunStatus.Should().Be("lost");
    }

    [Fact]
    public async Task ReportCombatResult_Resolved_BroadcastsTheFreshSnapshotOnceToTheRunGroup()
    {
        // Act
        await ReportAsync(Request(CombatOutcome.Won));

        // Assert
        _mediatorMock.Verify(mediator =>
            mediator.Send(new GetDungeonRunByIdQuery(RunId), It.IsAny<CancellationToken>())
        );
        _groupMock.Verify(group => group.DungeonStateChanged(_snapshot), Times.Once);
        _groupMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReportCombatResult_SeveralHeroesAndMobs_KeepsThemAllInOrder()
    {
        // Arrange
        Guid otherHero = Guid.NewGuid();
        Guid otherMob = Guid.NewGuid();
        ReportCombatResultRequest request = Request(CombatOutcome.Fled);
        request.Heroes.Add(
            new HeroFinalState
            {
                HeroId = otherHero.ToString(),
                CurrentHp = 0,
                CurrentMana = 0,
                KnockedOut = true,
            }
        );
        request.DefeatedElementIds.Add(otherMob.ToString());

        // Act
        await ReportAsync(request);

        // Assert
        _sent!.Heroes.Select(hero => hero.HeroId).Should().Equal(HeroId, otherHero);
        _sent.DefeatedElementIds.Should().Equal(MobId, otherMob);
    }

    [Theory]
    [InlineData(CombatOutcome.Unspecified)]
    [InlineData((CombatOutcome)99)]
    public async Task ReportCombatResult_UnknownOutcome_IsAnInvalidArgumentWithoutAnyCall(
        CombatOutcome outcome
    )
    {
        // Act
        Func<Task> act = () => ReportAsync(Request(outcome));

        // Assert
        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should()
            .Be(StatusCode.InvalidArgument);
        _mediatorMock.VerifyNoOtherCalls();
        _groupMock.VerifyNoOtherCalls();
    }

    public static TheoryData<string, string> MalformedIds() =>
        new()
        {
            { "command_id", "not-a-uuid" },
            { "command_id", Guid.Empty.ToString() },
            { "combat_id", "" },
            { "combat_id", Guid.Empty.ToString() },
            { "run_id", "42" },
            { "run_id", Guid.Empty.ToString() },
            { "encounter_id", "not-a-uuid" },
            { "encounter_id", Guid.Empty.ToString() },
            { "hero_id", "not-a-uuid" },
            { "hero_id", Guid.Empty.ToString() },
            { "defeated_id", "not-a-uuid" },
            { "defeated_id", Guid.Empty.ToString() },
        };

    [Theory]
    [MemberData(nameof(MalformedIds))]
    public async Task ReportCombatResult_MalformedId_IsAnInvalidArgumentWithoutAnyCall(
        string field,
        string value
    )
    {
        // Arrange
        ReportCombatResultRequest request = Request(CombatOutcome.Won);
        switch (field)
        {
            case "command_id":
                request.CommandId = value;
                break;
            case "combat_id":
                request.CombatId = value;
                break;
            case "run_id":
                request.RunId = value;
                break;
            case "encounter_id":
                request.EncounterId = value;
                break;
            case "hero_id":
                request.Heroes[0].HeroId = value;
                break;
            case "defeated_id":
                request.DefeatedElementIds[0] = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }

        // Act
        Func<Task> act = () => ReportAsync(request);

        // Assert
        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should()
            .Be(StatusCode.InvalidArgument);
        _mediatorMock.VerifyNoOtherCalls();
        _groupMock.VerifyNoOtherCalls();
    }

    private Task<ReportCombatResultResponse> ReportAsync(ReportCombatResultRequest request) =>
        _service.ReportCombatResult(
            request,
            new FakeServerCallContext(TestContext.Current.CancellationToken)
        );

    private static ReportCombatResultRequest Request(CombatOutcome outcome)
    {
        ReportCombatResultRequest request = new()
        {
            CommandId = CommandId.ToString(),
            CombatId = CombatId.ToString(),
            RunId = RunId.ToString(),
            EncounterId = EncounterId.ToString(),
            Outcome = outcome,
        };
        request.Heroes.Add(
            new HeroFinalState
            {
                HeroId = HeroId.ToString(),
                CurrentHp = 42,
                CurrentMana = 7,
                KnockedOut = true,
            }
        );
        request.DefeatedElementIds.Add(MobId.ToString());
        return request;
    }
}
