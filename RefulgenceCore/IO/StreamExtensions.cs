using System.Buffers;
using System.Runtime.InteropServices;

namespace Refulgence.IO;

public static class StreamExtensions
{
    public static void Write<T>(this Stream stream, in T value) where T : unmanaged
        => Write(stream, new ReadOnlySpan<T>(in value));

    public static void Write<T>(this Stream stream, ReadOnlySpan<T> value) where T : unmanaged
        => stream.Write(MemoryMarshal.AsBytes(value));

    public static bool TryGetSegment(this Stream stream, int length, out ArraySegment<byte> segment)
    {
        if (stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer)) {
            segment = buffer[(int)stream.Position..];
            if (length < segment.Count) {
                segment = segment[..length];
            }

            return true;
        }

        segment = default;
        return false;
    }

    public static void ReadFully(this Stream stream, byte[] buffer, int offset, int length)
    {
        while (length > 0) {
            var read = stream.Read(buffer, offset, length);
            if (read == 0) {
                throw new EndOfStreamException();
            }

            offset += read;
            length -= read;
        }
    }

    public static void PadToAlignment(this Stream stream, int alignment, byte padding)
    {
        var misalignment = (int)(stream.Length % alignment);
        if (misalignment == 0) {
            return;
        }

        var paddingLength = alignment - misalignment;
        stream.WriteRepeat(paddingLength, padding);
    }

    public static void WriteRepeat(this Stream stream, int count, byte value)
    {
        if (count <= 0) {
            return;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(count);
        try {
            buffer.AsSpan(..count).Fill(value);
            stream.Write(buffer, 0, count);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static void Reserve(this Stream stream, long minimumRemainingLength)
    {
        var position = stream.Position;
        var remainingLength = stream.Length - position;
        if (remainingLength < minimumRemainingLength) {
            stream.SetLength(position + minimumRemainingLength);
        }
    }
}
