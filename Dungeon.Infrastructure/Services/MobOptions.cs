using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;

namespace Dungeon.Infrastructure.Services;

/// <summary>
/// The behaviour of each kind of mob (DEPLACEMENT_MOBS.fr.md, section 5). Every value has a
/// default, so a deployment only lists what it tunes.
/// </summary>
public sealed class MobOptions
{
    public const string SectionName = "Dungeon:Mobs";

    public int WanderRadius { get; init; } = MobRules.Default.WanderRadius;

    public MobKindOptions Zombie { get; init; } = MobKindOptions.From(EnemyKind.Zombie);

    public MobKindOptions Bat { get; init; } = MobKindOptions.From(EnemyKind.Bat);

    public MobKindOptions Goblin { get; init; } = MobKindOptions.From(EnemyKind.Goblin);

    public MobKindOptions Mage { get; init; } = MobKindOptions.From(EnemyKind.Mage);

    /// <summary>Throws when a value is invalid, so that the service does not start with it.</summary>
    public MobRules ToRules() =>
        new(
            WanderRadius,
            new Dictionary<EnemyKind, MobBehaviour>
            {
                [EnemyKind.Zombie] = Zombie.ToBehaviour(),
                [EnemyKind.Bat] = Bat.ToBehaviour(),
                [EnemyKind.Goblin] = Goblin.ToBehaviour(),
                [EnemyKind.Mage] = Mage.ToBehaviour(),
            }
        );
}

public sealed class MobKindOptions
{
    public string Cadence { get; set; } = "1";

    public int? AggroRange { get; set; }

    public MobStep Step { get; set; }

    public MobIdle Idle { get; set; }

    public int TeleportDistance { get; set; }

    public static MobKindOptions From(EnemyKind kind)
    {
        MobBehaviour behaviour = MobRules.Default.For(kind);
        return new MobKindOptions
        {
            Cadence = behaviour.Cadence,
            AggroRange = behaviour.AggroRange,
            Step = behaviour.Step,
            Idle = behaviour.Idle,
            TeleportDistance = behaviour.TeleportDistance,
        };
    }

    public MobBehaviour ToBehaviour() => new(Cadence, AggroRange, Step, Idle, TeleportDistance);
}
