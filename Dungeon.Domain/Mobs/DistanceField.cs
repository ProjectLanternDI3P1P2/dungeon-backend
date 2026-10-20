using Dungeon.Domain.Entities;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Mobs;

/// <summary>
/// How many steps every walkable tile of a floor lies from an origin, by breadth-first
/// search. Mobs walk down it to reach the hero; a diagonal step never cuts a wall corner.
/// </summary>
// TODO(tech-debt): one search over the whole floor per turn (two with bats) — limit it to a
// radius or reuse it between turns if a floor grows past a few thousand tiles.
internal sealed class DistanceField
{
    /// <summary>North, east, south, west, then the diagonals: the order ties are broken in.</summary>
    private static readonly (int Dx, int Dy)[] Steps =
    [
        (0, -1),
        (1, 0),
        (0, 1),
        (-1, 0),
        (1, -1),
        (1, 1),
        (-1, 1),
        (-1, -1),
    ];

    private readonly DungeonFloor _floor;
    private readonly int[] _steps;

    private DistanceField(DungeonFloor floor, int[] steps)
    {
        _floor = floor;
        _steps = steps;
    }

    public static DistanceField From(
        DungeonFloor floor,
        Position origin,
        bool diagonal,
        Func<Position, bool>? allowed = null
    )
    {
        int[] steps = new int[floor.Width * floor.Height];
        Array.Fill(steps, -1);
        steps[origin.Y * floor.Width + origin.X] = 0;

        Queue<Position> queue = new([origin]);
        while (queue.Count > 0)
        {
            Position current = queue.Dequeue();
            int next = steps[current.Y * floor.Width + current.X] + 1;
            foreach (Position neighbour in Neighbours(floor, current, diagonal, allowed))
            {
                int index = neighbour.Y * floor.Width + neighbour.X;
                if (steps[index] < 0)
                {
                    steps[index] = next;
                    queue.Enqueue(neighbour);
                }
            }
        }

        return new DistanceField(floor, steps);
    }

    /// <summary>The walkable tiles one step away, in tie-breaking order.</summary>
    public static IEnumerable<Position> Neighbours(
        DungeonFloor floor,
        Position from,
        bool diagonal,
        Func<Position, bool>? allowed = null
    )
    {
        bool Open(Position tile) =>
            floor.Contains(tile) && floor.IsWalkable(tile) && (allowed?.Invoke(tile) ?? true);

        foreach ((int dx, int dy) in diagonal ? Steps : Steps[..4])
        {
            Position tile = new(from.X + dx, from.Y + dy);
            bool cutsACorner =
                dx != 0
                && dy != 0
                && !(
                    Open(new Position(from.X + dx, from.Y))
                    && Open(new Position(from.X, from.Y + dy))
                );
            if (Open(tile) && !cutsACorner)
            {
                yield return tile;
            }
        }
    }

    /// <summary>Steps from the origin, or null when the tile cannot be reached.</summary>
    public int? To(Position tile)
    {
        if (!_floor.Contains(tile))
        {
            return null;
        }

        int steps = _steps[tile.Y * _floor.Width + tile.X];
        return steps < 0 ? null : steps;
    }
}
