using System.Security.Cryptography;
using System.Text;

namespace Dungeon.Application.Features.DungeonRunUseCase.OpenChest;

/// <summary>Derives a stable Reward identity for a generated dungeon chest.</summary>
/// <remarks>
/// This is an identifier derivation, not a security hash. SHA-256 keeps the value
/// deterministic across retries without relying on SHA-1.
/// The resulting UUID uses the RFC 9562 version 8 custom format.
/// </remarks>
public static class ChestRewardIdentity
{
    public static Guid For(Guid runId, int floor, int elementId)
    {
        Span<byte> namespaceBytes = stackalloc byte[16];
        runId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);
        byte[] nameBytes = Encoding.UTF8.GetBytes($"{floor}:{elementId}");
        byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(input);
        nameBytes.CopyTo(input.AsSpan(namespaceBytes.Length));

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);
        Span<byte> uuidBytes = hash[..16];
        uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x80);
        uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);
        return new Guid(uuidBytes, bigEndian: true);
    }
}
