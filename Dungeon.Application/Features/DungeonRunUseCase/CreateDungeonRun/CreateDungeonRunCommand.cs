using Dungeon.Application.Abstractions;

namespace Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;

/// <summary>
/// Starts the run of a game session. Idempotent: a game session has one run, so a retry
/// with the same <paramref name="RunId"/> or another one changes nothing once it exists.
/// </summary>
/// <param name="RunId">Chosen by the caller, so that a retried request is idempotent (ADR-0025).</param>
/// <param name="Seed">Null for a new dungeon; an existing seed to replay a shared adventure.</param>
public record CreateDungeonRunCommand(Guid RunId, Guid GameSessionId, string? Seed) : ICommand;
