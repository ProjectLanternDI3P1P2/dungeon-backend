namespace Dungeon.Infrastructure.Grpc.Configuration;

public sealed class CombatGrpcClientOptions
{
    public const string SectionName = "Grpc:Combat";

    public string Address { get; init; } = "http://localhost:8081";
    public int TimeoutSeconds { get; init; } = 5;
}
