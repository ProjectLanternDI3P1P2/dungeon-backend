using Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Contracts.V1;
using Dungeon.Presentation.Grpc.Services;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Grpc.Core;
using MediatR;
using Moq;
using Serilog.Core;

namespace Dungeon.Test.Presentation.Grpc;

public class DungeonRunGrpcServiceTests
{
    private static readonly Guid CommandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HeroId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DungeonRunGrpcService _service;

    public DungeonRunGrpcServiceTests()
    {
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(
                    It.IsAny<GetDungeonRunByGameSessionIdQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new GetDungeonRunByIdResult
                {
                    Id = CommandId,
                    GameSessionId = SessionId,
                    Seed = "0KX4M2T9QZ7PA",
                }
            );

        _service = new DungeonRunGrpcService(_mediatorMock.Object, Logger.None);
    }

    [Fact]
    public async Task CreateDungeonRun_ValidRequest_StartsTheRunAndReturnsItsIdAndSeed()
    {
        // Act
        CreateDungeonRunResponse response = await _service.CreateDungeonRun(
            Request(),
            new FakeServerCallContext(TestContext.Current.CancellationToken)
        );

        // Assert
        response.RunId.Should().Be(CommandId.ToString());
        response.Seed.Should().Be("0KX4M2T9QZ7PA");
        _mediatorMock.Verify(mediator =>
            mediator.Send(
                new CreateDungeonRunCommand(CommandId, SessionId, null),
                It.IsAny<CancellationToken>()
            )
        );
        _mediatorMock.Verify(mediator =>
            mediator.Send(
                new GetDungeonRunByGameSessionIdQuery(SessionId),
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task CreateDungeonRun_WithASeed_ReplaysThatDungeon()
    {
        // Arrange
        CreateDungeonRunRequest request = Request();
        request.Seed = "0KX4M2T9QZ7PA";

        // Act
        await _service.CreateDungeonRun(
            request,
            new FakeServerCallContext(TestContext.Current.CancellationToken)
        );

        // Assert
        _mediatorMock.Verify(mediator =>
            mediator.Send(
                new CreateDungeonRunCommand(CommandId, SessionId, "0KX4M2T9QZ7PA"),
                It.IsAny<CancellationToken>()
            )
        );
    }

    public static TheoryData<string, string> MalformedIds() =>
        new()
        {
            { "command_id", "not-a-uuid" },
            { "command_id", Guid.Empty.ToString() },
            { "game_session_id", "" },
            { "hero_id", "42" },
        };

    [Theory]
    [MemberData(nameof(MalformedIds))]
    public async Task CreateDungeonRun_MalformedId_IsAnInvalidArgument(string field, string value)
    {
        // Arrange
        CreateDungeonRunRequest request = Request();
        switch (field)
        {
            case "command_id":
                request.CommandId = value;
                break;
            case "game_session_id":
                request.GameSessionId = value;
                break;
            default:
                request.Participants[0].HeroId = value;
                break;
        }

        // Act & Assert
        await ShouldBeAnInvalidArgumentAsync(request);
    }

    [Fact]
    public async Task CreateDungeonRun_NoParticipant_IsAnInvalidArgument()
    {
        // Arrange
        CreateDungeonRunRequest request = Request();
        request.Participants.Clear();

        // Act & Assert
        await ShouldBeAnInvalidArgumentAsync(request);
    }

    [Fact]
    public async Task CreateDungeonRun_HeroListedTwice_IsAnInvalidArgument()
    {
        // Arrange
        CreateDungeonRunRequest request = Request();
        request.Participants.Add(new DungeonRunParticipant { HeroId = HeroId.ToString() });

        // Act & Assert
        await ShouldBeAnInvalidArgumentAsync(request);
    }

    private async Task ShouldBeAnInvalidArgumentAsync(CreateDungeonRunRequest request)
    {
        // Act
        Func<Task> act = () =>
            _service.CreateDungeonRun(
                request,
                new FakeServerCallContext(TestContext.Current.CancellationToken)
            );

        // Assert
        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should()
            .Be(StatusCode.InvalidArgument);
        _mediatorMock.Verify(
            mediator =>
                mediator.Send(It.IsAny<CreateDungeonRunCommand>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private static CreateDungeonRunRequest Request() =>
        new()
        {
            CommandId = CommandId.ToString(),
            GameSessionId = SessionId.ToString(),
            Participants = { new DungeonRunParticipant { HeroId = HeroId.ToString() } },
        };
}
