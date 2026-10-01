# Dungeon.Contracts

Versioned Protocol Buffers contract and generated C# gRPC client/server types for
the Dungeon service.

## Installation

Configure the organisation's GitHub Packages NuGet source, then pin a released
version of the package:

```xml
<PackageReference Include="Dungeon.Contracts" Version="3.0.0" />
```

For a C# gRPC consumer, also reference `Grpc.Net.Client` (or
`Grpc.Net.ClientFactory`) and create a channel for the Dungeon service's internal
endpoint.

The original `.proto` sources are included in this package under `proto/`.

## Services

| Service | Proto | Client |
| --- | --- | --- |
| `DungeonRunService` | `dungeon_run_v1.proto` | `Dungeon.Contracts.V1.DungeonRunService.DungeonRunServiceClient` |
| `DungeonPlayerService` | `dungeon_player_v1.proto` | `Dungeon.Contracts.V1.DungeonPlayerService.DungeonPlayerServiceClient` |

### `DungeonRunService/CreateDungeonRun`

Player calls it when the creator of a game session starts it. Dungeon generates
the dungeon and starts the run; the response carries the `run_id` and the `seed`
to keep on the game session.

- `command_id`: the idempotency key of the Player command (UUID). It becomes the
  id of the run when the call creates one.
- `game_session_id`: the Player game session (UUID).
- `participants`: the heroes entering the dungeon, at least one, each once.
- `seed`: empty for a new dungeon, or a seed of a previous run to replay it.

The call is idempotent per game session: a retry, with the same `command_id` or
another one, returns the run already started for the session. It is therefore
safe to retry after a deadline.

| Status | When |
| --- | --- |
| `INVALID_ARGUMENT` | A malformed id, no participant, a hero listed twice, an invalid seed. |
| `ALREADY_EXISTS` | `command_id` already started the run of another game session. |
| `UNAVAILABLE`, `DEADLINE_EXCEEDED` | Transient: retry with the same `command_id`. |

```csharp
var client = new DungeonRunService.DungeonRunServiceClient(channel);
CreateDungeonRunResponse run = await client.CreateDungeonRunAsync(
    new CreateDungeonRunRequest
    {
        CommandId = commandId.ToString(),
        GameSessionId = sessionId.ToString(),
        Participants = { new DungeonRunParticipant { HeroId = heroId.ToString() } },
    },
    deadline: DateTime.UtcNow.AddSeconds(2),
    cancellationToken: cancellationToken
);
```
