using System.Reflection;
using System.Runtime.InteropServices;
using Org.BouncyCastle.Crypto.Digests;

namespace Refulgence.Dxbc;

// https://github.com/GPUOpen-Archive/common-src-ShaderUtils/blob/master/DX10/DXBCChecksum.cpp
internal static class DxDigest
{
    public const int Length = 0x10;

    private static readonly byte[] Padding = new byte[64];
    private static readonly Type   Md5Type = typeof(MD5Digest);

    static DxDigest()
    {
        Padding[0] = 0x80;
    }

    public static bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> digest)
    {
        if (digest.Length != 16) {
            throw new ArgumentException($"{nameof(digest)} must be 16 bytes long");
        }

        Span<byte> calculatedDigest = stackalloc byte[16];
        Calculate(data, calculatedDigest);
        return digest.SequenceEqual(calculatedDigest);
    }

    public static void Calculate(ReadOnlySpan<byte> data, Span<byte> digest)
    {
        if (digest.Length != 16) {
            throw new ArgumentException($"{nameof(digest)} must be 16 bytes long");
        }

        var md5 = new MD5Digest();

        var fullChunkSize = data.Length & ~63;
        md5.BlockUpdate(data[..fullChunkSize]);

        // Proprietary finish.
        var numberOfBits = (uint)data.Length << 3;
        var lastChunk = data[fullChunkSize..];
        if (lastChunk.Length >= 56) {
            md5.BlockUpdate(lastChunk);
            md5.BlockUpdate(Padding.AsSpan(0, 64 - lastChunk.Length));
            Span<uint> @in = stackalloc uint[16];
            @in[0] = numberOfBits;
            @in[15] = (numberOfBits >> 2) | 1;
            md5.BlockUpdate(MemoryMarshal.AsBytes(@in));
        } else {
            md5.BlockUpdate(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(in numberOfBits)));
            if (lastChunk.Length > 0) {
                md5.BlockUpdate(lastChunk);
            }
            md5.BlockUpdate(Padding.AsSpan(0, 56 - lastChunk.Length));
            var last = (numberOfBits >> 2) | 1;
            md5.BlockUpdate(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(in last)));
        }

        // Skip MD5's standard finish.
        CopyOutput(md5, MemoryMarshal.Cast<byte, uint>(digest));
    }

    private static void CopyOutput(MD5Digest md5, Span<uint> digest)
    {
        digest[0] = GetOutput(md5, "H1");
        digest[1] = GetOutput(md5, "H2");
        digest[2] = GetOutput(md5, "H3");
        digest[3] = GetOutput(md5, "H4");
    }

    private static uint GetOutput(MD5Digest md5, string component)
        => (uint)Md5Type.InvokeMember(
            component, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField, null, md5, null
        )!;
}
