using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Services.Randomness;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Mobs;

/// <summary>
/// What the mobs of the current floor do once the hero has moved (DEPLACEMENT_MOBS.fr.md,
/// section 3): the party discovers the room it stands in, mobs in range turn to aggro, every
/// active mob acts on its cadence, in element order, and those then next to the hero die.
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

        // Walking into the glow a mage announced is a contact too: the fight starts before the
        // mage can pick another tile.
        foreach (RunMob mage in active.Where(mob => mob.Teleport == hero))
        {
            mage.Kill();
        }

        foreach (RunMob mob in active.Where(mob => mob.IsActive))
        {
            MobBehaviour behaviour = rules.For(mob.Kind);
            bool diagonal = behaviour.Step == MobStep.Diagonal;
            if (mob.IsAggro && behaviour.Step == MobStep.Teleport)
            {
                // Announces on the turn before its action, so it acts off-cadence too.
                Teleport(mob, floor, walking, behaviour, hero, IsFree);
            }
            else if (!behaviour.ActsOn(mob.Tick))
            {
                // Its cadence lets it rest this turn.
            }
            else if (mob.IsAggro)
            {
                DistanceField field = diagonal
                    ? flying ??= DistanceField.From(floor, hero, diagonal: true)
                    : walking;
                Chase(mob, floor, field, diagonal, IsFree);
            }
            else if (!mob.IsAggro && behaviour.Idle == MobIdle.Wander)
            {
                Wander(mob, run, floor, rules.WanderRadius, diagonal, IsFree);
            }
            else if (
                !mob.IsAggro
                && behaviour.Idle == MobIdle.NearestDoor
                && run.DiscoveredRoomIds.Contains(mob.HomeRoomId)
            )
            {
                HoldTheNearestDoor(mob, floor, IsFree);
            }

            mob.NextTick();
        }

        // TODO(tech-debt): contact kills the mob in place of a fight — call Combat with its pack
        // (fight-trigger user story).
        foreach (RunMob mob in active.Where(mob => mob.Position.DistanceTo(hero) <= 1))
        {
            mob.Kill();
        }

        bool IsFree(Position tile) =>
            tile != hero && !active.Any(other => other.IsActive && other.Position == tile);
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

    /// <summary>
    /// On the turn before it acts, a mage announces the tile it will jump to; when it acts, it
    /// lands there if the tile is free, otherwise it stays and announces another one.
    /// </summary>
    private static void Teleport(
        RunMob mob,
        DungeonFloor floor,
        DistanceField field,
        MobBehaviour behaviour,
        Position hero,
        Func<Position, bool> isFree
    )
    {
        if (behaviour.ActsOn(mob.Tick))
        {
            if (mob.Teleport is Position landing && isFree(landing))
            {
                mob.MoveTo(landing);
                mob.Announce(null);
            }
            else
            {
                mob.Announce(
                    JumpTarget(mob, floor, field, behaviour.TeleportDistance, hero, isFree)
                );
            }
        }
        else if (behaviour.ActsOn(mob.Tick + 1))
        {
            mob.Announce(JumpTarget(mob, floor, field, behaviour.TeleportDistance, hero, isFree));
        }
    }

    /// <summary>
    /// The best tile within <paramref name="distance"/> steps of the mage, diagonals included:
    /// the free one closest to the hero along the path, then as the crow flies, then in reading
    /// order. Steps never cross nor cut a wall, and never end on the hero. Null when no tile
    /// brings the mage closer.
    /// </summary>
    private static Position? JumpTarget(
        RunMob mob,
        DungeonFloor floor,
        DistanceField towardHero,
        int distance,
        Position hero,
        Func<Position, bool> isFree
    )
    {
        DistanceField reach = DistanceField.From(floor, mob.Position, diagonal: true);
        Position? best = null;
        (int Steps, int Crow) bestScore = (towardHero.To(mob.Position) ?? int.MaxValue, 0);
        for (int y = mob.Y - distance; y <= mob.Y + distance; y++)
        {
            for (int x = mob.X - distance; x <= mob.X + distance; x++)
            {
                Position tile = new(x, y);
                if (
                    reach.To(tile) is >= 1 and int jump
                    && jump <= distance
                    && tile != hero
                    && isFree(tile)
                    && towardHero.To(tile) is int steps
                )
                {
                    (int, int) score = (steps, tile.DistanceTo(hero));
                    if (score.CompareTo(bestScore) < 0)
                    {
                        best = tile;
                        bestScore = score;
                    }
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Back toward the centre of its room while farther than the wander radius, otherwise a
    /// random step, or none, without leaving the room.
    /// </summary>
    private static void Wander(
        RunMob mob,
        DungeonRun run,
        DungeonFloor floor,
        int radius,
        bool diagonal,
        Func<Position, bool> isFree
    )
    {
        bool InItsRoom(Position tile) => floor.GetRoomId(tile) == mob.HomeRoomId;
        Position centre = floor.Rooms[mob.HomeRoomId].Center;

        if (mob.Position.DistanceTo(centre) > radius)
        {
            DistanceField towardCentre = DistanceField.From(floor, centre, diagonal, InItsRoom);
            Chase(mob, floor, towardCentre, diagonal, isFree);
            return;
        }

        List<Position?> choices =
        [
            null,
            .. DistanceField
                .Neighbours(floor, mob.Position, diagonal, InItsRoom)
                .Where(isFree)
                .Select(tile => (Position?)tile),
        ];
        DeterministicRandom random = DeterministicRandom.ForMobTurn(
            run.Seed,
            run.CurrentFloor,
            run.Turn,
            mob.ElementId
        );
        if (random.Pick(choices) is Position step)
        {
            mob.MoveTo(step);
        }
    }

    /// <summary>
    /// Heads for the door of its room nearest to it (ties: reading order) and stands on it.
    /// </summary>
    private static void HoldTheNearestDoor(
        RunMob mob,
        DungeonFloor floor,
        Func<Position, bool> isFree
    )
    {
        bool InItsRoomOrDoor(Position tile) =>
            floor.GetRoomId(tile) == mob.HomeRoomId || floor.GetCell(tile) == CellType.Door;

        if (mob.Door is not Position door)
        {
            RoomBounds inside = floor.Rooms[mob.HomeRoomId].Interior;
            DistanceField fromMob = DistanceField.From(
                floor,
                mob.Position,
                diagonal: false,
                InItsRoomOrDoor
            );
            Position? nearest = null;
            int nearestSteps = int.MaxValue;
            for (int y = inside.Y - 1; y <= inside.Bottom + 1; y++)
            {
                for (int x = inside.X - 1; x <= inside.Right + 1; x++)
                {
                    Position tile = new(x, y);
                    if (
                        floor.Contains(tile)
                        && floor.GetCell(tile) == CellType.Door
                        && fromMob.To(tile) is int steps
                        && steps < nearestSteps
                    )
                    {
                        nearest = tile;
                        nearestSteps = steps;
                    }
                }
            }

            if (nearest is not Position found)
            {
                return;
            }

            mob.TargetDoor(found);
            door = found;
        }

        if (mob.Position != door)
        {
            DistanceField towardDoor = DistanceField.From(
                floor,
                door,
                diagonal: false,
                InItsRoomOrDoor
            );
            Chase(mob, floor, towardDoor, diagonal: false, isFree);
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
