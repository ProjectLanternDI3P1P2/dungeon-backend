using Dungeon.Application.Models;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;
using FluentAssertions;

namespace Dungeon.Test.Features.Models;

/// <summary>What the "kind" of an element says on the wire: an enemy's, or a decoration's.</summary>
public class DungeonElementResultTests
{
    private static DungeonElement Element(
        ElementType type,
        EnemyKind? kind = null,
        DecorationKind? decoration = null
    ) => new(Guid.NewGuid(), type, new Position(3, 4), 7, kind, decoration);

    [Theory]
    [InlineData(EnemyKind.Zombie, "zombie")]
    [InlineData(EnemyKind.Mage, "mage")]
    [InlineData(EnemyKind.Goblin, "goblin")]
    [InlineData(EnemyKind.Bat, "bat")]
    public void From_Enemy_SaysItsKind(EnemyKind kind, string expected)
    {
        DungeonElementResult.From(Element(ElementType.Enemy, kind)).Kind.Should().Be(expected);
    }

    [Theory]
    [InlineData(DecorationKind.Ship, "ship")]
    [InlineData(DecorationKind.Campfire, "campfire")]
    [InlineData(DecorationKind.Strawberry, "strawberry")]
    [InlineData(DecorationKind.GrassBlock, "grassBlock")]
    [InlineData(DecorationKind.BluePortal, "bluePortal")]
    [InlineData(DecorationKind.OrangePortal, "orangePortal")]
    public void From_Decoration_SaysWhatItShows(DecorationKind decoration, string expected)
    {
        DungeonElementResult
            .From(Element(ElementType.Decoration, decoration: decoration))
            .Kind.Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(ElementType.Boss)]
    [InlineData(ElementType.Item)]
    [InlineData(ElementType.Trap)]
    public void From_AnyOtherElement_HasNoKind(ElementType type)
    {
        DungeonElementResult.From(Element(type)).Kind.Should().BeNull();
    }

    [Fact]
    public void From_KeepsTheRestOfTheElement()
    {
        DungeonElement element = Element(ElementType.Trap);

        DungeonElementResult result = DungeonElementResult.From(element);

        result.Should().Be(new DungeonElementResult(element.Id, "trap", 3, 4, 7, null));
    }
}
