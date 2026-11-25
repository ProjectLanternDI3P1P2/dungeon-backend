using Dungeon.Application.Models;

namespace Dungeon.Application.Ports;

/// <summary>Application port reading the appearance of heroes, owned by the Player service.</summary>
public interface IHeroAppearanceClient
{
    /// <returns>
    /// The appearance chosen in the hero editor, or null when Player does not know the hero,
    /// cannot be reached, or describes it in a form Dungeon cannot pass on.
    /// </returns>
    Task<HeroAppearance?> GetAppearanceAsync(Guid heroId, CancellationToken cancellationToken);
}
