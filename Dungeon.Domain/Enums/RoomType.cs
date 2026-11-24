namespace Dungeon.Domain.Enums;

public enum RoomType
{
    /// <summary>Where the party arrives on a floor.</summary>
    Start,
    Combat,
    Treasure,

    /// <summary>A quiet room: no enemy, no loot. Gives the exploration some rhythm.</summary>
    Empty,

    /// <summary>
    /// The last room of every floor. Its boss guards the gate in its north wall, the way down
    /// to the next floor; on the last floor, it is the final boss.
    /// </summary>
    Boss,

    /// <summary>
    /// An easter-egg room, a tribute to a game the class loves: decorations only, no enemy,
    /// no loot. Legendary: a floor hides one only once in a long while (see
    /// <see cref="Services.Generation.EasterEggs"/>).
    /// </summary>
    Secret,
}
