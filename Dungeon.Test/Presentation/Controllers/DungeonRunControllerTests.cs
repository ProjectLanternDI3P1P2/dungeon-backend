using Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Presentation.Controllers;
using Dungeon.Presentation.DTO;
using Dungeon.Presentation.Hubs;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog.Core;

namespace Dungeon.Test.Presentation.Controllers;

/// <summary>
/// The party of a run created over REST: nothing proves a hero is the caller's, so only the
/// development launcher may name them (the real flow is Player's gRPC call).
/// </summary>
public class DungeonRunControllerTests
{
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HeroA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid HeroB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IHostEnvironment> _environmentMock = new();
    private CreateDungeonRunCommand? _sent;

    public DungeonRunControllerTests()
    {
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<CreateDungeonRunCommand>(), It.IsAny<CancellationToken>())
            )
            .Callback<CreateDungeonRunCommand, CancellationToken>((command, _) => _sent = command)
            .Returns(Task.CompletedTask);
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(
                    It.IsAny<GetDungeonRunByGameSessionIdQuery>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new GetDungeonRunByIdResult { Id = Guid.NewGuid() });
    }

    [Fact]
    public async Task Create_InDevelopment_HonorsTheHeroIdsWithoutEmptyIdsAndDuplicates()
    {
        // Act
        await CreateAsync(Environments.Development, [HeroA, Guid.Empty, HeroB, HeroA]);

        // Assert
        _sent!.HeroIds.Should().Equal(HeroA, HeroB);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task Create_OutsideDevelopment_IgnoresTheHeroIds(string environmentName)
    {
        // Act
        await CreateAsync(environmentName, [HeroA, HeroB]);

        // Assert
        _sent!.HeroIds.Should().BeNull();
        _sent.GameSessionId.Should().Be(SessionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task Create_InDevelopmentWithoutHeroIds_StartsARunWithoutParty(int? count)
    {
        // Act
        await CreateAsync(
            Environments.Development,
            count is null ? null : [.. Enumerable.Repeat(HeroA, count.Value)]
        );

        // Assert
        _sent!.HeroIds.Should().BeNull();
    }

    private Task CreateAsync(string environmentName, List<Guid>? heroIds)
    {
        _environmentMock
            .SetupGet(environment => environment.EnvironmentName)
            .Returns(environmentName);
        DungeonRunController controller = new(
            _mediatorMock.Object,
            new Mock<IHubContext<DungeonHub, IDungeonClient>>().Object,
            _environmentMock.Object,
            Logger.None
        );

        return controller.Create(
            new CreateDungeonRunDto { GameSessionId = SessionId, HeroIds = heroIds },
            TestContext.Current.CancellationToken
        );
    }
}
