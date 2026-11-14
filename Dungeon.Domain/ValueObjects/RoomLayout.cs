namespace Dungeon.Domain.ValueObjects;

/// <summary>Where a room stands: on the coarse room grid, on the tile grid, and its middle.</summary>
/// <param name="GridCell">Coordinates on the coarse room grid, used for the minimap.</param>
/// <param name="Interior">The walkable interior of the room, walls excluded.</param>
/// <param name="Center">The tile every corridor of the room lines up with. Never blocked.</param>
public readonly record struct RoomLayout(Position GridCell, RoomBounds Interior, Position Center);
