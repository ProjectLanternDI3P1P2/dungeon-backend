using Dungeon.Contracts.Events.V1;
using Google.Protobuf;

namespace Dungeon.Application.Messaging;

/// <summary>Creates the integration fact published after a chest is confirmed open.</summary>
public static class ChestOpenedMessageFactory
{
    public const string MessageType = "dungeon.chest.opened";
    public const int Version = 1;

    public static MessageEnvelope Create(
        Guid runId,
        Guid gameSessionId,
        string seed,
        int floor,
        int elementId,
        DateTimeOffset openedAt
    )
    {
        ChestOpened payload = new()
        {
            RunId = runId.ToString(),
            GameSessionId = gameSessionId.ToString(),
            Seed = seed,
            Floor = floor,
            ElementId = elementId,
            OpenedAtUtc = openedAt.ToString("O"),
        };

        Guid messageId = Guid.NewGuid();
        return new MessageEnvelope(
            messageId,
            runId.ToString(),
            null,
            MessageType,
            Version,
            openedAt,
            "dungeon",
            payload.ToByteArray()
        );
    }
}
