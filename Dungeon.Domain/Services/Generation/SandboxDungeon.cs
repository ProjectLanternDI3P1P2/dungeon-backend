using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Services.Generation;

/// <summary>
/// A debug dungeon behind a reserved seed: two bare rooms, one above the other, joined by a
/// straight corridor, with no element, on every floor. The party arrives in the south one.
/// Mobs are spawned in them by hand (<see cref="DungeonRun.SpawnMob"/>) to watch how each
/// kind moves.
/// </summary>
public static class SandboxDungeon
{
    public static readonly Seed Seed = Seed.Parse("0000000SANDBX");

    private const int InteriorWidth = 21;
    private const int InteriorHeight = 15;
    private const int CorridorLength = 6;

    // Void around the walls: the client draws wall faces and rims there.
    private const int Margin = 3;

    public static GeneratedDungeon Build(DungeonSettings settings, int generatorVersion)
    {
        return new GeneratedDungeon(
            Seed,
            generatorVersion,
            settings,
            [
                .. Enumerable
                    .Range(0, settings.FloorCount)
                    .Select(index => BuildFloor(index, index == settings.FloorCount - 1)),
            ]
        )
        {
            IsSandbox = true,
        };
    }

    private static DungeonFloor BuildFloor(int index, bool isFinalFloor)
    {
        RoomBounds north = new(Margin + 1, Margin + 1, InteriorWidth, InteriorHeight);
        // Below the north room: its wall, the corridor, then the wall of the south room.
        RoomBounds south = north with
        {
            Y = north.Bottom + CorridorLength + 3,
        };
        int width = north.Right + Margin + 2;
        int height = south.Bottom + Margin + 2;
        int column = north.X + InteriorWidth / 2;

        CellType[] cells = new CellType[width * height];
        foreach (RoomBounds room in (RoomBounds[])[north, south])
        {
            for (int y = room.Y; y <= room.Bottom; y++)
            {
                for (int x = room.X; x <= room.Right; x++)
                {
                    cells[y * width + x] = CellType.Floor;
                }
            }
        }

        for (int y = north.Bottom + 2; y < south.Y - 1; y++)
        {
            cells[y * width + column] = CellType.Floor;
        }

        cells[(north.Bottom + 1) * width + column] = CellType.Door;
        cells[(south.Y - 1) * width + column] = CellType.Door;
        RaiseWalls(cells, width, height);

        Position southCenter = new(column, south.Y + InteriorHeight / 2);
        Position northCenter = new(column, north.Y + InteriorHeight / 2);
        // Ids index the list: the start room is 0.
        Room[] rooms =
        [
            new(0, RoomType.Start, new Position(0, 1), south, southCenter, 0, [1]),
            new(1, RoomType.Empty, new Position(0, 0), north, northCenter, 1, [0]),
        ];
        return new DungeonFloor(index, isFinalFloor, width, height, cells, rooms, [], southCenter);
    }

    /// <summary>Every void tile touching a room, a door or the corridor, diagonals included, becomes a wall.</summary>
    private static void RaiseWalls(CellType[] cells, int width, int height)
    {
        bool IsInside(int x, int y) =>
            x >= 0
            && y >= 0
            && x < width
            && y < height
            && cells[y * width + x] is not (CellType.Void or CellType.Wall);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (
                    cells[y * width + x] == CellType.Void
                    && Enumerable
                        .Range(0, 9)
                        .Any(around => IsInside(x + around % 3 - 1, y + around / 3 - 1))
                )
                {
                    cells[y * width + x] = CellType.Wall;
                }
            }
        }
    }
}
