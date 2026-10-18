using Dungeon.Domain.Enums;

namespace Dungeon.Domain.Services.Generation.Templates;

/// <summary>
/// The rooms where enemies wait; the generator places them, the templates draw only the
/// room. Those also listed as <see cref="RoomType.Empty"/> serve as empty rooms too.
/// See <see cref="RoomTemplates"/> for the legend.
/// </summary>
internal static class CombatRoomTemplates
{
    public static IReadOnlyList<RoomTemplate> All { get; } =
    [
        // Two rows of columns along the long walls.
        new(
            "Colonnade",
            [RoomType.Combat],
            [
                "...............",
                "..I..I...I..I..",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
                "..I..I...I..I..",
                "...............",
            ]
        ),
        // Two walled pits; the way across is the middle aisle.
        new(
            "TwinPits",
            [RoomType.Combat, RoomType.Empty],
            [
                ".................",
                ".................",
                "..     ...     ..",
                "..     ...     ..",
                "..     ...     ..",
                "..     ...     ..",
                "..     ...     ..",
                ".......t.t.......",
                ".................",
                ".................",
                ".................",
            ]
        ),
        // Cross-shaped hall, jars in the arms.
        new(
            "Cross",
            [RoomType.Combat],
            [
                "    .......    ",
                "    ..o.o..    ",
                "    .......    ",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
                "    .......    ",
                "    ..o.o..    ",
                "    .......    ",
            ]
        ),
        // A colonnade splits off a side chamber. Walls one tile thick read as flat slabs in
        // three-quarter view: columns divide the room instead.
        new(
            "SideChamber",
            [RoomType.Combat],
            [
                ".....I...........",
                ".................",
                ".....I...........",
                "...........ooo...",
                ".....I...........",
                ".................",
                ".....I...........",
                ".................",
                ".....I...........",
                ".o...........t...",
                ".....I.......t...",
            ]
        ),
        // A thick wall closes off a northern cell with a single passage.
        new(
            "NorthCell",
            [RoomType.Combat, RoomType.Empty],
            [
                "...............",
                ".o...........o.",
                "...............",
                "#######.#######",
                "#######.#######",
                "...............",
                "...............",
                "...............",
                "......t.t......",
                "...............",
                "oo...........oo",
            ]
        ),
        // Spike traps guard both sides of the room.
        new(
            "SpikeLanes",
            [RoomType.Combat],
            [
                ".................",
                ".................",
                ".................",
                "..ttt.......ttt..",
                ".................",
                "..ttt.......ttt..",
                ".................",
                ".................",
                ".................",
            ]
        ),
        // Sewer grates in the middle, four columns around them.
        new(
            "GrateChamber",
            [RoomType.Combat],
            [
                "...............",
                "...............",
                "...............",
                "...I.......I...",
                "...............",
                ".....ggggg.....",
                ".....ggggg.....",
                ".....ggggg.....",
                "...............",
                "...I.......I...",
                "...............",
                "...............",
                "...............",
            ]
        ),
        // Supplies stacked along the walls.
        new(
            "Barracks",
            [RoomType.Combat],
            [
                "oo.o.....o.oo",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                "oo.o.....o.oo",
            ]
        ),
        // The south side is a railing over the void.
        new(
            "Balcony",
            [RoomType.Combat],
            [
                "...............",
                ".I...........I.",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
                "======...======",
            ]
        ),
        // A small room, two ambushers and a pair of spikes.
        new(
            "Closet",
            [RoomType.Combat],
            [
                "o.......o",
                ".........",
                ".........",
                ".........",
                "...t.t...",
                ".........",
                "o.......o",
            ]
        ),
        // L-shaped room: one quarter has caved in.
        new(
            "Corner",
            [RoomType.Combat],
            [
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                "........     ",
                "........     ",
                "..o.....     ",
            ]
        ),
        // A forest of columns.
        new(
            "Hypostyle",
            [RoomType.Combat],
            [
                ".................",
                ".I...I.....I...I.",
                ".................",
                ".................",
                ".I...I.....I...I.",
                ".................",
                ".I...I.....I...I.",
                ".................",
                ".................",
                ".I...I.....I...I.",
                ".................",
            ]
        ),
        // Four little chapels of columns on the sides.
        new(
            "Galleries",
            [RoomType.Combat],
            [
                ".................",
                ".I.I.........I.I.",
                ".................",
                ".I.I.........I.I.",
                ".......ooo.......",
                ".................",
                ".................",
                ".I.I.........I.I.",
                ".................",
                ".I.I.........I.I.",
                ".................",
            ]
        ),
        // Four pools of still water; the fight goes on around them.
        new(
            "FloodedHall",
            [RoomType.Combat, RoomType.Empty],
            [
                ".................",
                ".................",
                "..~~~~.....~~~~..",
                "..~~~~.....~~~~..",
                "..~~~~.....~~~~..",
                ".................",
                "..~~~~.....~~~~..",
                "..~~~~.....~~~~..",
                "..~~~~.....~~~~..",
                ".................",
                ".................",
            ]
        ),
        // Rows of tombs; something stirs between them.
        new(
            "Crypt",
            [RoomType.Combat, RoomType.Empty],
            [
                "...............",
                ".TTT.TT.TT.TTT.",
                "...............",
                "...............",
                "...............",
                "...............",
                ".TTT.TT.TT.TTT.",
                "...............",
                "..o.........o..",
            ]
        ),
        // A labyrinth of thick walls and dead ends.
        new(
            "Labyrinth",
            [RoomType.Combat, RoomType.Empty],
            [
                ".##................",
                ".#####.########.##.",
                ".#####.########.##.",
                "....##.......##....",
                ".##.##.###########.",
                ".##.##.###########.",
                ".##.##.##....##....",
                ".##.#####.##.##.##.",
                ".##.#####.##.##.##.",
                ".##.......##....##.",
                ".##.##############.",
                ".##.##############.",
                "...................",
            ],
            RoomRarity.Rare
        ),
        // A much larger labyrinth, rarer still, with traps in its turns.
        new(
            "DeepLabyrinth",
            [RoomType.Combat],
            [
                ".##.............##.......",
                ".#####.########.##.#####.",
                ".#####.########.##.#####.",
                ".##....##.......##....##.",
                ".##.#####.##############.",
                ".##.#####.##############.",
                ".##....##................",
                ".#####.###########.##.##.",
                ".#####.###########.##.##.",
                ".......##.............##.",
                "######.##.#####.##.##.##.",
                "######.##.#####.##.##.##.",
                ".......##.##....##....##.",
                "###.##.##.##.########.##.",
                "###.##.##.##.########.##.",
                "....##.##.##.......##.##.",
                ".########.########.#####.",
                ".########.########.#####.",
                ".........................",
            ],
            RoomRarity.Rare
        ),
        // A hall shaped as a cross, columns at its heart.
        new(
            "CrossHall",
            [RoomType.Combat],
            [
                "       .......       ",
                "       .......       ",
                "       .......       ",
                "       .......       ",
                "       .......       ",
                "........I...I........",
                ".....................",
                ".....................",
                ".....................",
                "........I...I........",
                "       .......       ",
                "       .......       ",
                "       .......       ",
                "       .......       ",
                "       .......       ",
            ]
        ),
        // A room cut as a diamond: narrow at the ends, wide in the middle.
        new(
            "Diamond",
            [RoomType.Combat, RoomType.Empty],
            [
                "        .        ",
                "       ...       ",
                "     .......     ",
                "    .........    ",
                "   ...........   ",
                " ............... ",
                ".................",
                " ............... ",
                "   ...........   ",
                "    ....t....    ",
                "     .......     ",
                "       ...       ",
                "        .        ",
            ],
            RoomRarity.Uncommon
        ),
        // Two moats split the hall; causeways cross them.
        new(
            "MoatHall",
            [RoomType.Combat, RoomType.Empty],
            [
                "...................",
                "...................",
                "...................",
                "...~~~~~~.~~~~~~...",
                "...~~~~~~.~~~~~~...",
                "...~~~~~~.~~~~~~...",
                "...................",
                "...~~~~~~.~~~~~~...",
                "...~~~~~~.~~~~~~...",
                "...~~~~~~.~~~~~~...",
                "...................",
                ".o.................",
                "...................",
            ],
            RoomRarity.Uncommon
        ),
        // Thick walls from either side: the way snakes through the room.
        new(
            "Serpent",
            [RoomType.Combat],
            [
                "...................",
                "...................",
                "##########.........",
                "##########.........",
                "...................",
                "...................",
                "...................",
                ".........##########",
                ".........##########",
                "...................",
                "...................",
            ],
            RoomRarity.Uncommon
        ),
        // A forest of columns, open along its two middle lines.
        new(
            "PillarForest",
            [RoomType.Combat],
            [
                ".....................",
                ".I..I..I.....I..I..I.",
                ".....................",
                ".I..I..I.....I..I..I.",
                ".....................",
                ".I..I..I.....I..I..I.",
                ".....................",
                ".....................",
                ".....................",
                ".I..I..I.....I..I..I.",
                ".....................",
                ".I..I..I.....I..I..I.",
                ".....................",
                ".I..I..I.....I..I..I.",
                ".....................",
            ],
            RoomRarity.Uncommon
        ),
        // A small guardroom.
        new(
            "Guardroom",
            [RoomType.Combat],
            ["o.....o", ".......", ".......", ".......", "o.....o"]
        ),
        // A long hall lined with columns.
        new(
            "LongHall",
            [RoomType.Combat, RoomType.Empty],
            [
                ".........................",
                ".I...I...I...I...I...I...",
                ".........................",
                ".........................",
                ".........................",
                "...I...I...I...I...I...I.",
                ".........................",
            ]
        ),
        // Rows of tombs with alleys between them.
        new(
            "Catacombs",
            [RoomType.Combat, RoomType.Empty],
            [
                "...................",
                ".TTT.TT.TTT.TT.TTT.",
                "...................",
                ".TT.TTT.....TTT.TT.",
                "...................",
                ".TTT.TT.....TT.TTT.",
                "...................",
                ".TT.TTT.TTT.TTT.TT.",
                "...................",
            ],
            RoomRarity.Uncommon
        ),
        // A flooded hall: causeways meet on an island in the middle.
        new(
            "SunkenBridges",
            [RoomType.Combat],
            [
                ".....................",
                ".~~~~~~~~~.~~~~~~~~~.",
                ".~~~~~~~~~.~~~~~~~~~.",
                ".~~~~~~~~~.~~~~~~~~~.",
                ".~~~~~~.......~~~~~~.",
                ".~~~~~~.......~~~~~~.",
                "...t.............t...",
                ".~~~~~~.......~~~~~~.",
                ".~~~~~~.......~~~~~~.",
                ".~~~~~~~~~.~~~~~~~~~.",
                ".~~~~~~~~~.~~~~~~~~~.",
                ".~~~~~~~~~.~~~~~~~~~.",
                ".....................",
            ],
            RoomRarity.Rare
        ),
        // Two wings joined by a broad hall and a passage through the middle.
        new(
            "TwinWings",
            [RoomType.Combat],
            [
                ".....  ...  .....",
                ".....  ...  .....",
                ".....  ...  .....",
                ".................",
                "..o.........o....",
                ".................",
                ".....  ...  .....",
                ".....  ...  .....",
                ".....  ...  .....",
            ]
        ),
        // Square pillars form alcoves along the north and south walls.
        new(
            "Alcoves",
            [RoomType.Combat, RoomType.Empty],
            [
                ".....................",
                ".##..##..##.##..##..#",
                ".##..##..##.##..##..#",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".....................",
                ".##..##..##.##..##..#",
                ".##..##..##.##..##..#",
                ".....................",
            ]
        ),
    ];
}
