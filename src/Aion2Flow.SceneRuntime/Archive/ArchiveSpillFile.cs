using System.IO.Compression;
using System.Runtime.InteropServices;
using Cloris.Aion2Flow.SceneRuntime.Stores;

namespace Cloris.Aion2Flow.SceneRuntime.Archive;

/// <summary>
/// Disk spill for the heavy per-fight event arrays (T2.3). Records are unmanaged structs, so they are
/// written as raw bytes through Brotli (no reflection, AOT-safe). Format: magic, start ordinal, three counts, Brotli body.
/// </summary>
internal static class ArchiveSpillFile
{
    private const uint Magic = 0x41534631; // "ASF1"

    internal static void Write(string path, long combatStart, ReadOnlySpan<CombatEventRecord> combat, ReadOnlySpan<CombatMechanicEventRecord> mechanic, ReadOnlySpan<CombatResourceEventRecord> resource)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096);
        Span<byte> header = stackalloc byte[28];
        BitConverter.TryWriteBytes(header, Magic);
        BitConverter.TryWriteBytes(header[4..], combatStart);
        BitConverter.TryWriteBytes(header[12..], combat.Length);
        BitConverter.TryWriteBytes(header[16..], mechanic.Length);
        BitConverter.TryWriteBytes(header[20..], resource.Length);
        file.Write(header[..24]);
        using var brotli = new BrotliStream(file, CompressionLevel.Fastest);
        brotli.Write(MemoryMarshal.AsBytes(combat));
        brotli.Write(MemoryMarshal.AsBytes(mechanic));
        brotli.Write(MemoryMarshal.AsBytes(resource));
    }

    internal static bool TryRead(string path, out long combatStart, out CombatEventRecord[] combat, out CombatMechanicEventRecord[] mechanic, out CombatResourceEventRecord[] resource)
    {
        combatStart = 0;
        combat = [];
        mechanic = [];
        resource = [];
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
            Span<byte> header = stackalloc byte[24];
            file.ReadExactly(header);
            if (BitConverter.ToUInt32(header) != Magic)
                return false;

            combatStart = BitConverter.ToInt64(header[4..]);
            combat = new CombatEventRecord[BitConverter.ToInt32(header[12..])];
            mechanic = new CombatMechanicEventRecord[BitConverter.ToInt32(header[16..])];
            resource = new CombatResourceEventRecord[BitConverter.ToInt32(header[20..])];
            using var brotli = new BrotliStream(file, CompressionMode.Decompress);
            brotli.ReadExactly(MemoryMarshal.AsBytes(combat.AsSpan()));
            brotli.ReadExactly(MemoryMarshal.AsBytes(mechanic.AsSpan()));
            brotli.ReadExactly(MemoryMarshal.AsBytes(resource.AsSpan()));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or EndOfStreamException)
        {
            combat = [];
            mechanic = [];
            resource = [];
            return false;
        }
    }
}
