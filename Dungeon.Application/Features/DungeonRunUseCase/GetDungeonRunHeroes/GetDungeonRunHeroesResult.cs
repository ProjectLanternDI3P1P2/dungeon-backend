namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunHeroes;

public sealed class GetDungeonRunHeroesResult
{
    public Guid RunId { get; init; }

    /// <summary>The heroes of the party, in the order Player listed them.</summary>
    public IReadOnlyList<RunHeroResult> Heroes { get; init; } = [];
}
