extern alias PlayerContracts;
using Dungeon.Application.Models;
using Dungeon.Infrastructure.Grpc.Clients;
using Dungeon.Infrastructure.Grpc.Configuration;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PlayerContracts::Player.Contracts.V1;

namespace Dungeon.Test.Infrastructure.Grpc;

public class PlayerHeroGrpcClientTests
{
    private readonly Mock<PlayerHeroService.PlayerHeroServiceClient> _serviceMock = new();
    private readonly PlayerHeroGrpcClient _client;

    public PlayerHeroGrpcClientTests()
    {
        _client = new PlayerHeroGrpcClient(
            _serviceMock.Object,
            Options.Create(new PlayerGrpcClientOptions()),
            NullLogger<PlayerHeroGrpcClient>.Instance
        );
    }

    [Fact]
    public async Task GetAppearanceAsync_ChosenAppearance_MapsTheReply()
    {
        // Arrange
        Guid heroId = Guid.NewGuid();
        Answer(
            new HeroAppearanceReply
            {
                HeroId = heroId.ToString(),
                SkinTone = 4,
                HairStyle = "dap1",
                HairColor = 7,
            }
        );

        // Act
        HeroAppearance? appearance = await _client.GetAppearanceAsync(
            heroId,
            TestContext.Current.CancellationToken
        );

        // Assert
        appearance.Should().Be(new HeroAppearance(4, "dap1", 7));
        _serviceMock.Verify(service =>
            service.GetHeroAppearanceAsync(
                It.Is<GetHeroAppearanceRequest>(request => request.HeroId == heroId.ToString()),
                It.IsAny<CallOptions>()
            )
        );
    }

    [Fact]
    public async Task GetAppearanceAsync_BaldHero_HasNoHairColor()
    {
        // Arrange: Player leaves hair_color absent for a bald hero.
        Guid heroId = Guid.NewGuid();
        Answer(
            new HeroAppearanceReply
            {
                HeroId = heroId.ToString(),
                SkinTone = 2,
                HairStyle = "bald",
            }
        );

        // Act
        HeroAppearance? appearance = await _client.GetAppearanceAsync(
            heroId,
            TestContext.Current.CancellationToken
        );

        // Assert
        appearance.Should().Be(new HeroAppearance(2, "bald", null));
    }

    [Theory]
    [InlineData(StatusCode.NotFound)]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Internal)]
    public async Task GetAppearanceAsync_PlayerFails_ReturnsNull(StatusCode statusCode)
    {
        // Arrange
        Fail(new RpcException(new Status(statusCode, "failure")));

        // Act
        HeroAppearance? appearance = await _client.GetAppearanceAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken
        );

        // Assert
        appearance.Should().BeNull();
    }

    [Theory]
    [InlineData("another-hero", 3, "bob1")]
    [InlineData(null, 0, "bob1")]
    [InlineData(null, 3, "")]
    public async Task GetAppearanceAsync_MalformedReply_ReturnsNull(
        string? repliedHeroId,
        int skinTone,
        string hairStyle
    )
    {
        // Arrange: null stands for the requested hero's own id.
        Guid heroId = Guid.NewGuid();
        Answer(
            new HeroAppearanceReply
            {
                HeroId = repliedHeroId ?? heroId.ToString(),
                SkinTone = skinTone,
                HairStyle = hairStyle,
                HairColor = 1,
            }
        );

        // Act
        HeroAppearance? appearance = await _client.GetAppearanceAsync(
            heroId,
            TestContext.Current.CancellationToken
        );

        // Assert
        appearance.Should().BeNull();
    }

    [Fact]
    public async Task GetAppearanceAsync_RequestCancelled_LetsTheCancellationThrough()
    {
        // Arrange
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        Fail(new RpcException(new Status(StatusCode.Cancelled, "cancelled")));

        // Act
        Func<Task> act = () => _client.GetAppearanceAsync(Guid.NewGuid(), cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<RpcException>();
    }

    private void Answer(HeroAppearanceReply reply) => Setup(Task.FromResult(reply));

    private void Fail(RpcException exception) =>
        Setup(Task.FromException<HeroAppearanceReply>(exception));

    private void Setup(Task<HeroAppearanceReply> response) =>
        _serviceMock
            .Setup(service =>
                service.GetHeroAppearanceAsync(
                    It.IsAny<GetHeroAppearanceRequest>(),
                    It.IsAny<CallOptions>()
                )
            )
            .Returns(
                new AsyncUnaryCall<HeroAppearanceReply>(
                    response,
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => [],
                    () => { }
                )
            );
}
