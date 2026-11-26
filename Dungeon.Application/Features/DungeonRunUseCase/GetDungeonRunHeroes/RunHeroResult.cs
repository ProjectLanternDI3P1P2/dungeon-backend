using Dungeon.Application.Models;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunHeroes;

/// <param name="IsDefaultAppearance">
/// Player could not describe the hero (unknown, unreachable or malformed answer): the
/// appearance is <see cref="HeroAppearance.Default"/>.
/// </param>
public sealed record RunHeroResult(
    Guid HeroId,
    HeroAppearance Appearance,
    bool IsDefaultAppearance
);
