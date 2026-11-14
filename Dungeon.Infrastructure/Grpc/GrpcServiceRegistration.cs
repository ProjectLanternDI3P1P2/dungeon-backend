using Dungeon.Contracts.V1;
using Dungeon.Infrastructure.Grpc.Clients;
using Dungeon.Infrastructure.Grpc.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reward.Contracts.V1;

namespace Dungeon.Infrastructure.Grpc;

public static class GrpcServiceRegistration
{
    public static IServiceCollection AddGrpcConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        PlayerGrpcClientOptions playerGrpcClientOptions =
            configuration
                .GetSection(PlayerGrpcClientOptions.SectionName)
                .Get<PlayerGrpcClientOptions>()
            ?? new PlayerGrpcClientOptions();
        RewardGrpcClientOptions rewardGrpcClientOptions =
            configuration
                .GetSection(RewardGrpcClientOptions.SectionName)
                .Get<RewardGrpcClientOptions>()
            ?? new RewardGrpcClientOptions();

        ValidatePlayerGrpcClientOptions(playerGrpcClientOptions);
        ValidateRewardGrpcClientOptions(rewardGrpcClientOptions);

        services
            .AddSingleton(Options.Create(playerGrpcClientOptions))
            .AddSingleton(Options.Create(rewardGrpcClientOptions))
            .AddGrpcClient<DungeonPlayerService.DungeonPlayerServiceClient>(options =>
                options.Address = new Uri(playerGrpcClientOptions.Address)
            )
            .Services.AddGrpcClient<RewardLootService.RewardLootServiceClient>(options =>
                options.Address = new Uri(rewardGrpcClientOptions.Address)
            );

        return services.Scan(scan =>
            scan.FromAssembliesOf(typeof(GrpcServiceRegistration))
                .AddClasses(classes => classes.Where(c => c.Name.EndsWith("GrpcClient")))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );
    }

    private static void ValidatePlayerGrpcClientOptions(PlayerGrpcClientOptions options)
    {
        if (!Uri.TryCreate(options.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Grpc:Player:Address must be an absolute URI.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Grpc:Player:TimeoutSeconds must be greater than zero."
            );
        }
    }

    private static void ValidateRewardGrpcClientOptions(RewardGrpcClientOptions options)
    {
        if (!Uri.TryCreate(options.Address, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Grpc:Rewards:Address must be an absolute URI.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Grpc:Rewards:TimeoutSeconds must be greater than zero."
            );
        }
    }
}
