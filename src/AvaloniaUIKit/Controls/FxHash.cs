using System.Buffers.Binary;
using System.Text;

namespace AvaloniaUIKit;

/// <summary>
/// The hash GPUI's <c>gpui::hash</c> computes for a string: rustc-hash 2.1's
/// FxHasher over the UTF-8 bytes and the 0xFF terminator Rust's
/// <c>Hash for str</c> writes. GPUI Kit picks an avatar's color from it, so
/// a name gets the same color here.
/// </summary>
internal static class FxHash
{
    private const ulong K = 0xf1357aea2e62a9c5;
    private const ulong Seed1 = 0x243f6a8885a308d3;
    private const ulong Seed2 = 0x13198a2e03707344;
    private const ulong PreventTrivialZeroCollapse = 0xa4093822299f31d0;

    public static ulong Hash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ulong hash = 0;
        hash = (hash + HashBytes(bytes)) * K;
        hash = (hash + 0xff) * K;
        return (hash << 26) | (hash >> 38);
    }

    private static ulong MultiplyMix(ulong x, ulong y)
    {
        var full = (UInt128)x * y;
        return (ulong)full ^ (ulong)(full >> 64);
    }

    private static ulong ReadU64(ReadOnlySpan<byte> b) => BinaryPrimitives.ReadUInt64LittleEndian(b);

    private static ulong ReadU32(ReadOnlySpan<byte> b) => BinaryPrimitives.ReadUInt32LittleEndian(b);

    private static ulong HashBytes(ReadOnlySpan<byte> bytes)
    {
        var len = bytes.Length;
        var s0 = Seed1;
        var s1 = Seed2;
        if (len <= 16)
        {
            if (len >= 8)
            {
                s0 ^= ReadU64(bytes);
                s1 ^= ReadU64(bytes[(len - 8)..]);
            }
            else if (len >= 4)
            {
                s0 ^= ReadU32(bytes);
                s1 ^= ReadU32(bytes[(len - 4)..]);
            }
            else if (len > 0)
            {
                s0 ^= bytes[0];
                s1 ^= ((ulong)bytes[len - 1] << 8) | bytes[len / 2];
            }
        }
        else
        {
            var bulk = bytes[..(len - 1)];
            while (bulk.Length >= 16)
            {
                var x = ReadU64(bulk);
                var y = ReadU64(bulk[8..]);
                var t = MultiplyMix(s0 ^ x, PreventTrivialZeroCollapse ^ y);
                s0 = s1;
                s1 = t;
                bulk = bulk[16..];
            }
            var suffix = bytes[(len - 16)..];
            s0 ^= ReadU64(suffix);
            s1 ^= ReadU64(suffix[8..]);
        }
        return MultiplyMix(s0, s1) ^ (ulong)len;
    }
}
