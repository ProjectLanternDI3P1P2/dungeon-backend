using Dungeon.Domain.Enums;
using Dungeon.Domain.Services.Randomness;

namespace Dungeon.Domain.Services.Generation;

/// <summary>
/// The legendary roll: once in a long while, an empty room of a floor becomes an
/// easter-egg room (<see cref="RoomType.Secret"/>). The odds are configured: one floor in
/// <c>oneIn</c>; 0 turns easter eggs off.
/// <para>
/// The roll has its own random stream, and a floor without an easter egg is built exactly
/// as before: only the seeds that roll one look different. Changing the odds changes which
/// seeds those are.
/// </para>
/// </summary>
public static class EasterEggs
{
    /// <summary>One floor in a hundred: about one run in thirty, on four floors.</summary>
    public const int DefaultOneIn = 100;

    internal static void Roll(DeterministicRandom random, List<RoomDraft> rooms, int oneIn)
    {
        if (oneIn <= 0 || random.NextInt(oneIn) != 0)
        {
            return;
        }

        List<RoomDraft> empties = rooms.Where(room => room.Type == RoomType.Empty).ToList();
        if (empties.Count > 0)
        {
            random.Pick(empties).Type = RoomType.Secret;
        }
    }
}
