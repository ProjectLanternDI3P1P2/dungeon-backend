using System.Security.Cryptography;
using System.Text;

namespace Dungeon.Application.Features.DungeonRunUseCase.OpenChest;

/// <summary>Derives the stable Reward identity for a generated dungeon chest.</summary>
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

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(input, hash);
        Span<byte> uuidBytes = hash[..16];
        uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x50);
        uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);
        return new Guid(uuidBytes, bigEndian: true);
    }
}
