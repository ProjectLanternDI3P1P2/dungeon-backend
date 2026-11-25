namespace Dungeon.Domain.Enums;

/// <summary>What a <see cref="ElementType.Decoration"/> element shows, in an easter-egg room.</summary>
public enum DecorationKind
{
    /// <summary>The little ship of Outer Wilds, landed. Three tiles wide, two deep.</summary>
    Ship,

    /// <summary>Its campfire, with a marshmallow on a stick.</summary>
    Campfire,

    /// <summary>A winged strawberry of Celeste, floating.</summary>
    Strawberry,

    /// <summary>A Minecraft grass block.</summary>
    GrassBlock,

    /// <summary>The blue portal of Portal, hanging on the north wall like a tapestry.</summary>
    BluePortal,

    /// <summary>The orange portal, on the same wall.</summary>
    OrangePortal,
}
