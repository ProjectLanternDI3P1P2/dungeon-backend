using Dungeon.Domain.Entities;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

public class DistanceFieldTests
{
    private static DungeonFloor Floor(params string[] rows) =>
        DungeonTestData
            .Arena(rows, new RoomBounds(1, 1, rows[0].Length - 2, rows.Length - 2))
            .Floors[0];

    [Fact]
    public void Orthogonal_CountsTheStepsOfTheShortestPath()
    {
        // Arrange
        DungeonFloor floor = Floor("#####", "#H..#", "##..#", "#####");

        // Act
        DistanceField field = DistanceField.From(floor, new Position(1, 1), diagonal: false);

        // Assert
        field.To(new Position(1, 1)).Should().Be(0);
        field.To(new Position(3, 2)).Should().Be(3);
    }

    [Fact]
    public void Diagonal_StepsAcrossAnOpenCorner()
    {
        // Arrange
        DungeonFloor floor = Floor("####", "#H.#", "#..#", "####");

        // Act
        DistanceField field = DistanceField.From(floor, new Position(1, 1), diagonal: true);

        // Assert
        field.To(new Position(2, 2)).Should().Be(1);
    }

    [Fact]
    public void Diagonal_NeverCutsAWallCorner()
    {
        // Arrange: (2, 1) is a wall, so (1, 1) -> (2, 2) would cut its corner.
        DungeonFloor floor = Floor("####", "#H##", "#..#", "####");

        // Act
        DistanceField field = DistanceField.From(floor, new Position(1, 1), diagonal: true);

        // Assert
        field.To(new Position(2, 2)).Should().Be(2);
    }

    [Fact]
    public void To_AWall_IsNull()
    {
        // Arrange
        DungeonFloor floor = Floor("####", "#H.#", "####");

        // Act
        DistanceField field = DistanceField.From(floor, new Position(1, 1), diagonal: false);

        // Assert
        field.To(new Position(0, 0)).Should().BeNull();
    }
}
