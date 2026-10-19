namespace Dungeon.Domain.Enums;

/// <summary>
/// How an enemy silhouette moves on the map. Only the silhouette is decided here: the
/// pack of monsters it stands for, their stats and abilities belong to Combat.
/// </summary>
public enum EnemyKind
{
    /// <summary>Slow, walks straight at the hero.</summary>
    Zombie,

    /// <summary>Teleports three tiles at a time; particles announce where it lands next.</summary>
    Mage,

    /// <summary>Sneaky: heads for a door, an exit or a corridor and blocks it.</summary>
    Goblin,

    /// <summary>Flies diagonally.</summary>
    Bat,
}
