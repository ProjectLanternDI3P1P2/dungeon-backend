using System.Collections.Concurrent;
using Dungeon.Application.Models;
using Dungeon.Application.Ports;

namespace Dungeon.Test.TestSupport;

/// <summary>
/// Stands for Player in integration tests: it knows the heroes a test gives an appearance to
/// and, like an unreachable Player, describes no other.
/// </summary>
public sealed class FakeHeroAppearanceClient : IHeroAppearanceClient
{
    private readonly ConcurrentDictionary<Guid, HeroAppearance> _appearances = new();

    public void Choose(Guid heroId, HeroAppearance appearance) => _appearances[heroId] = appearance;

    public Task<HeroAppearance?> GetAppearanceAsync(
        Guid heroId,
        CancellationToken cancellationToken
    ) => Task.FromResult(_appearances.GetValueOrDefault(heroId));
}
