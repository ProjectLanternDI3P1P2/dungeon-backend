using Dungeon.Domain.Enums;

namespace Dungeon.Domain.Mobs;

/// <summary>How a mob moves when it acts.</summary>
public enum MobStep
{
    /// <summary>One tile north, east, south or west.</summary>
    Orthogonal,

    /// <summary>One tile in any of the eight directions, never cutting a wall corner.</summary>
    Diagonal,

    /// <summary>A jump along its path, to a tile announced the turn before.</summary>
    Teleport,
}

/// <summary>What a mob does before it is in aggro.</summary>
public enum MobIdle
{
    None,

    /// <summary>Walks to the centre of its room, then strolls at random without leaving it.</summary>
    Wander,

    /// <summary>Once its room is discovered, stands on the nearest door of the room.</summary>
    NearestDoor,
}

/// <summary>
/// The behaviour of one kind of mob. <see cref="Cadence"/> repeats, one character per turn:
/// <c>1</c> the mob acts, <c>0</c> it skips the turn ("110": two turns out of three).
/// A null <see cref="AggroRange"/> means aggro as soon as the room of the mob is discovered.
/// </summary>
public sealed record MobBehaviour
{
    public MobBehaviour(
        string cadence,
        int? aggroRange,
        MobStep step,
        MobIdle idle,
        int teleportDistance = 0
    )
    {
        if (string.IsNullOrEmpty(cadence) || cadence.Any(c => c is not ('0' or '1')))
        {
            throw new ArgumentException("A cadence is made of 0 and 1.", nameof(cadence));
        }

        if (!cadence.Contains('1'))
        {
            throw new ArgumentException("A cadence must let the mob act.", nameof(cadence));
        }

        if (aggroRange < 0)
        {
            throw new ArgumentException("An aggro range cannot be negative.", nameof(aggroRange));
        }

        if (step == MobStep.Teleport && teleportDistance < 1)
        {
            throw new ArgumentException(
                "A teleporting mob needs a distance of at least 1.",
                nameof(teleportDistance)
            );
        }

        Cadence = cadence;
        AggroRange = aggroRange;
        Step = step;
        Idle = idle;
        TeleportDistance = teleportDistance;
    }

    public string Cadence { get; }

    public int? AggroRange { get; }

    public MobStep Step { get; }

    public MobIdle Idle { get; }

    public int TeleportDistance { get; }

    /// <summary>Whether the mob acts on the given tick of its cadence.</summary>
    public bool ActsOn(int tick) => Cadence[tick % Cadence.Length] == '1';
}

/// <summary>The behaviour of every kind of mob, set by configuration (Dungeon:Mobs).</summary>
public sealed record MobRules
{
    public MobRules(int wanderRadius, IReadOnlyDictionary<EnemyKind, MobBehaviour> kinds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(wanderRadius);
        ArgumentNullException.ThrowIfNull(kinds);

        EnemyKind[] missing =
        [
            .. Enum.GetValues<EnemyKind>().Where(kind => !kinds.ContainsKey(kind)),
        ];
        if (missing.Length > 0)
        {
            throw new ArgumentException(
                $"No behaviour for {string.Join(", ", missing)}.",
                nameof(kinds)
            );
        }

        WanderRadius = wanderRadius;
        Kinds = kinds;
    }

    /// <summary>The values agreed in DEPLACEMENT_MOBS.fr.md, section 5.</summary>
    public static MobRules Default { get; } =
        new(
            2,
            new Dictionary<EnemyKind, MobBehaviour>
            {
                [EnemyKind.Zombie] = new("110", 10, MobStep.Orthogonal, MobIdle.Wander),
                [EnemyKind.Bat] = new("1110", 5, MobStep.Diagonal, MobIdle.Wander),
                [EnemyKind.Goblin] = new("1", 3, MobStep.Orthogonal, MobIdle.NearestDoor),
                [EnemyKind.Mage] = new("001", null, MobStep.Teleport, MobIdle.None, 3),
            }
        );

    /// <summary>How close to the centre of its room a wandering mob stays.</summary>
    public int WanderRadius { get; }

    public IReadOnlyDictionary<EnemyKind, MobBehaviour> Kinds { get; }

    public MobBehaviour For(EnemyKind kind) => Kinds[kind];
}
