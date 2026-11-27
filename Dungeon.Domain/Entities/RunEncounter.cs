using Dungeon.Domain.Enums;

namespace Dungeon.Domain.Entities;

/// <summary>
/// A contact between the party and a pack of 1 to N mobs (or the boss), fought in Combat.
/// The pack is frozen when the encounter starts: a mob arriving later does not join it.
/// Its id is the idempotency key of the CreateCombat call, so a retry never opens a second fight.
/// </summary>
public sealed class RunEncounter
{
    private List<Guid> _mobElementIds = [];

    // Required by EF Core.
    private RunEncounter() { }

    public Guid Id { get; private set; }

    /// <summary>0-based floor, as the run stores it.</summary>
    public int Floor { get; private set; }

    /// <summary>The room the hero stood in; null in a corridor.</summary>
    public int? RoomId { get; private set; }

    public EncounterTrigger Trigger { get; private set; }

    public EncounterStatus Status { get; private set; }

    /// <summary>The engaged mobs, nearest first.</summary>
    public IReadOnlyList<Guid> MobElementIds => _mobElementIds;

    /// <summary>The boss element, for a boss encounter.</summary>
    public Guid? BossElementId { get; private set; }

    public Guid? CombatId { get; private set; }

    public EncounterOutcome? Outcome { get; private set; }

    public int CreatedOnTurn { get; private set; }

    /// <summary>The turn the outcome was applied on: the snapshot shows it until the next turn.</summary>
    public int? ResolvedOnTurn { get; private set; }

    /// <summary>Not settled yet: the party is blocked and the mobs do not play.</summary>
    public bool IsOpen => Status != EncounterStatus.Resolved;

    internal static RunEncounter Start(
        EncounterTrigger trigger,
        int floor,
        int? roomId,
        IReadOnlyList<Guid> mobElementIds,
        Guid? bossElementId,
        int turn
    )
    {
        if (mobElementIds.Count == 0 && bossElementId is null)
        {
            throw new ArgumentException(
                "An encounter needs at least one foe.",
                nameof(mobElementIds)
            );
        }

        return new RunEncounter
        {
            Id = Guid.CreateVersion7(),
            Floor = floor,
            RoomId = roomId,
            Trigger = trigger,
            Status = EncounterStatus.Pending,
            _mobElementIds = [.. mobElementIds],
            BossElementId = bossElementId,
            CreatedOnTurn = turn,
        };
    }

    internal void CombatStarted(Guid combatId)
    {
        if (Status == EncounterStatus.Resolved)
        {
            return;
        }

        Status = EncounterStatus.Active;
        CombatId = combatId;
    }

    internal void CombatUnavailable()
    {
        if (Status is EncounterStatus.Pending or EncounterStatus.Failed)
        {
            Status = EncounterStatus.Failed;
        }
    }

    internal void Resolve(Guid combatId, EncounterOutcome outcome, int turn)
    {
        Status = EncounterStatus.Resolved;
        CombatId = combatId;
        Outcome = outcome;
        ResolvedOnTurn = turn;
    }
}
