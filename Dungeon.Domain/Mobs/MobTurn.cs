using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Mobs;

/// <summary>
/// What the mobs of the current floor do once the hero has moved (DEPLACEMENT_MOBS.fr.md,
/// section 3): the party discovers the room it stands in, mobs in range turn to aggro, then
/// every active mob acts on its cadence, in element order.
/// </summary>
// TODO(tech-debt): a single hero; the party of the multiplayer will need the nearest of several.
internal static class MobTurn
{
    public static void Play(DungeonRun run, DungeonFloor floor, MobRules rules)
    {
        Position hero = run.HeroPosition;
        Discover(run, floor, hero);

        DistanceField walking = DistanceField.From(floor, hero, diagonal: false);
        DistanceField? flying = null;

        List<RunMob> active = [.. run.Mobs.Where(mob => mob.IsActive)];
        foreach (RunMob mob in active.Where(mob => !mob.IsAggro))
        {
            int? range = rules.For(mob.Kind).AggroRange;
            bool inRange = range is int steps
                ? walking.To(mob.Position) <= steps
                : run.DiscoveredRoomIds.Contains(mob.HomeRoomId);
            if (inRange)
            {
                mob.Aggro();
            }
        }

        foreach (RunMob mob in active)
        {
            MobBehaviour behaviour = rules.For(mob.Kind);
            if (behaviour.ActsOn(mob.Tick) && mob.IsAggro)
            {
                switch (behaviour.Step)
                {
                    case MobStep.Orthogonal:
                        Chase(mob, floor, walking, diagonal: false, IsFree);
                        break;
                    case MobStep.Diagonal:
                        flying ??= DistanceField.From(floor, hero, diagonal: true);
                        Chase(mob, floor, flying, diagonal: true, IsFree);
                        break;
                }
            }

            mob.NextTick();
        }

        bool IsFree(Position tile) => tile != hero && !active.Any(other => other.Position == tile);
    }

    /// <summary>A room is discovered when a hero stands in it or on one of its doors.</summary>
    private static void Discover(DungeonRun run, DungeonFloor floor, Position hero)
    {
        if (floor.GetRoomId(hero) is int room)
        {
            run.Discover(room);
            return;
        }

        if (floor.GetCell(hero) != CellType.Door)
        {
            return;
        }

        foreach (Position side in DistanceField.Neighbours(floor, hero, diagonal: false))
        {
            if (floor.GetRoomId(side) is int doorRoom)
            {
                run.Discover(doorRoom);
            }
        }
    }

    /// <summary>One step down the field, to the first closer free tile; none, it waits.</summary>
    private static void Chase(
        RunMob mob,
        DungeonFloor floor,
        DistanceField field,
        bool diagonal,
        Func<Position, bool> isFree
    )
    {
        if (field.To(mob.Position) is not int here)
        {
            return;
        }

        Position? best = null;
        int bestSteps = here;
        foreach (Position next in DistanceField.Neighbours(floor, mob.Position, diagonal))
        {
            if (field.To(next) is int steps && steps < bestSteps && isFree(next))
            {
                best = next;
                bestSteps = steps;
            }
        }

        if (best is Position step)
        {
            mob.MoveTo(step);
        }
    }
}
