using Microsoft.Extensions.Caching.Memory;

namespace Dungeon.Presentation.Hubs;

/// <summary>
/// Remembers the answer to each accepted Hub command, so that a client retrying after a lost
/// acknowledgement gets the earlier answer instead of moving the hero a second time
/// (ADR-0025). In memory: Dungeon runs one replica in the MVP.
/// </summary>
public sealed class HubCommandCache(IMemoryCache cache)
{
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(10);

    private readonly Lock _gate = new();

    public async Task<DungeonCommandResult> RunOnceAsync(
        Guid commandId,
        Func<Task<DungeonCommandResult>> execute
    )
    {
        string key = $"dungeon-hub-command:{commandId}";
        Lazy<Task<DungeonCommandResult>> pending;

        // IMemoryCache.GetOrCreate is not atomic: two copies of a command sent at the same
        // time must share a single execution.
        lock (_gate)
        {
            pending = cache.GetOrCreate(
                key,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = Retention;
                    return new Lazy<Task<DungeonCommandResult>>(execute);
                }
            )!;
        }

        DungeonCommandResult? result = null;
        try
        {
            result = await pending.Value;
            return result;
        }
        finally
        {
            // A refused or failed command changed nothing: a retry is evaluated again.
            if (result is not { Accepted: true })
            {
                Forget(key, pending);
            }
        }
    }

    private void Forget(string key, Lazy<Task<DungeonCommandResult>> pending)
    {
        lock (_gate)
        {
            if (
                cache.TryGetValue(key, out Lazy<Task<DungeonCommandResult>>? current)
                && current == pending
            )
            {
                cache.Remove(key);
            }
        }
    }
}
