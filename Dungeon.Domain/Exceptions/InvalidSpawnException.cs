using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Exceptions;

/// <summary>A mob can only be spawned by hand in the sandbox, on a free floor tile of its room.</summary>
public sealed class InvalidSpawnException(Position target, string reason)
    : DomainException($"No mob can spawn at ({target.X}, {target.Y}): {reason}.");
