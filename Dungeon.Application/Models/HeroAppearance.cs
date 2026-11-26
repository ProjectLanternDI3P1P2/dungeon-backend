namespace Dungeon.Application.Models;

/// <summary>
/// What a hero looks like, as chosen in Player's hero editor. Player owns the catalogue of
/// codes; Dungeon hands them to the client untouched.
/// </summary>
/// <param name="SkinTone">Skin tone variant.</param>
/// <param name="HairStyle">Hair style code, for example <c>bald</c>, <c>bob1</c> or <c>dap1</c>.</param>
/// <param name="HairColor">Hair color variant; null for a bald hero.</param>
public sealed record HeroAppearance(int SkinTone, string HairStyle, int? HairColor)
{
    /// <summary>
    /// Shown when Player cannot describe the hero: the appearance Player itself gives a hero
    /// created without choosing one.
    /// </summary>
    public static HeroAppearance Default { get; } = new(1, "bob1", 1);
}
