using Dungeon.Domain.ValueObjects;
using FluentAssertions;

namespace Dungeon.Test.Domain.ValueObjects;

public class PositionTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 1)]
    [InlineData(-2, 1, 2)]
    [InlineData(3, -5, 5)]
    public void DistanceTo_CountsADiagonalStepAsOne(int dx, int dy, int expected)
    {
        // Arrange
        var origin = new Position(10, 10);

        // Act
        int distance = origin.DistanceTo(new Position(10 + dx, 10 + dy));

        // Assert
        distance.Should().Be(expected);
    }
}
