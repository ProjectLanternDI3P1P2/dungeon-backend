namespace Dungeon.Domain.ValueObjects;

/// <summary>The dimensions of a tile grid, in tiles.</summary>
public readonly record struct GridSize(int Width, int Height)
{
    public int Area => Width * Height;
}
