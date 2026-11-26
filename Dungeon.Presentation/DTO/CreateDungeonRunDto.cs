namespace Dungeon.Presentation.DTO;

public class CreateDungeonRunDto
{
    /// <summary>Optional idempotency key: Player sends it so that a retried call is harmless.</summary>
    public Guid? RunId { get; set; }

    public required Guid GameSessionId { get; set; }

    /// <summary>Omit it for a brand new dungeon; set it to replay a shared one.</summary>
    public string? Seed { get; set; }

    /// <summary>
    /// The heroes of the party. Without any, a contact with a mob never opens a fight (the
    /// historical stand-in kills the mob); Player sends them over gRPC for a game session.
    /// </summary>
    public List<Guid>? HeroIds { get; set; }
}
