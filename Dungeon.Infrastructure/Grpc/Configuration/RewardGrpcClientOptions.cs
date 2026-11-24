namespace Dungeon.Infrastructure.Grpc.Configuration;

public sealed class RewardGrpcClientOptions
{
    public const string SectionName = "Grpc:Rewards";

    public string Address { get; init; } = "http://localhost:8081";
    public int TimeoutSeconds { get; init; } = 5;
}
