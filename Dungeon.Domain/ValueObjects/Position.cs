using Dungeon.Domain.Enums;

namespace Dungeon.Domain.ValueObjects;

/// <summary>A tile coordinate on a dungeon floor. X grows eastwards, Y grows southwards.</summary>
public readonly record struct Position(int X, int Y)
{
    /// <summary>
    /// Chebyshev distance: a diagonal step counts as one, so the eight tiles around a
    /// position are all one tile away.
    /// </summary>
    public int DistanceTo(Position other)
    {
        return Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
    }

    public Position Step(Direction direction)
    {
        return direction switch
        {
            Direction.North => this with { Y = Y - 1 },
            Direction.East => this with { X = X + 1 },
            Direction.South => this with { Y = Y + 1 },
            Direction.West => this with { X = X - 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }
}
