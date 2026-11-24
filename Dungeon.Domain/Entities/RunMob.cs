using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Entities;

/// <summary>
/// An enemy silhouette of the current floor, as it stands during the run. It appears on its
/// generated element and moves from there (DEPLACEMENT_MOBS.fr.md, section 2).
/// </summary>
public sealed class RunMob
{
    // Required by EF Core.
    private RunMob() { }

    /// <summary>The id of the enemy element it spawned from: stable for the seed and floor.</summary>
    public Guid ElementId { get; private set; }

    public EnemyKind Kind { get; private set; }

    public int X { get; private set; }

    public int Y { get; private set; }

    /// <summary>The room it spawned in: where it wanders and which discovery wakes it.</summary>
    public int HomeRoomId { get; private set; }

    /// <summary>False once the party has touched it: it is dead and gone.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The turn it died on, so that the front can show it fall where it stood.</summary>
    public int? KilledOnTurn { get; private set; }

    /// <summary>Hunting the party, for good: aggro only ends with the mob or the floor.</summary>
    public bool IsAggro { get; private set; }

    /// <summary>Position in its cadence, counted from its spawn, then again from its aggro.</summary>
    public int Tick { get; private set; }

    public int? TeleportX { get; private set; }

    public int? TeleportY { get; private set; }

    public int? DoorX { get; private set; }

    public int? DoorY { get; private set; }

    public Position Position => new(X, Y);

    /// <summary>The tile a mage announced it will jump to on its next action.</summary>
    public Position? Teleport =>
        TeleportX is int x && TeleportY is int y ? new Position(x, y) : null;

    /// <summary>The door a goblin heads for.</summary>
    public Position? Door => DoorX is int x && DoorY is int y ? new Position(x, y) : null;

    internal static RunMob Spawn(DungeonElement enemy)
    {
        return new RunMob
        {
            ElementId = enemy.Id,
            Kind =
                enemy.Kind
                ?? throw new ArgumentException("Only an enemy spawns a mob.", nameof(enemy)),
            X = enemy.Position.X,
            Y = enemy.Position.Y,
            HomeRoomId = enemy.RoomId,
            IsActive = true,
        };
    }

    internal void MoveTo(Position position)
    {
        X = position.X;
        Y = position.Y;
    }

    internal void Aggro()
    {
        IsAggro = true;
        Tick = 0;
    }

    internal void NextTick() => Tick++;

    internal void Announce(Position? teleport)
    {
        TeleportX = teleport?.X;
        TeleportY = teleport?.Y;
    }

    internal void TargetDoor(Position door)
    {
        DoorX = door.X;
        DoorY = door.Y;
    }

    internal void Kill(int turn)
    {
        IsActive = false;
        KilledOnTurn = turn;
    }
}
