using System.Text;

namespace Refulgence.IO;

// mostly from: https://github.com/Ottermandias/Penumbra.GameData/
public sealed class StringPool : IDisposable
{
    public readonly MemoryStream Data = new();
    public readonly List<int>    StartingOffsets;

    public int Length
        => (int)Data.Length;

    public StringPool()
    {
        StartingOffsets = [];
    }

    public StringPool(ReadOnlySpan<byte> initialData)
    {
        Data.Write(initialData);
        StartingOffsets = [0,];
        for (var i = 0; i < initialData.Length; ++i) {
            if (initialData[i] == 0) {
                StartingOffsets.Add(i + 1);
            }
        }

        if (StartingOffsets[^1] == initialData.Length) {
            StartingOffsets.RemoveAt(StartingOffsets.Count - 1);
        } else {
            Data.WriteByte(0);
        }
    }

    public void Dispose()
        => Data.Dispose();

    public ReadOnlySpan<byte> AsSpan()
        => Data.GetBuffer().AsSpan()[..(int)Data.Length];

    public int WriteTo(Stream stream, int alignment = 1)
    {
        Data.WriteTo(stream);
        if (alignment <= 1) {
            return Length;
        }

        var offset = stream.Position % alignment;
        if (offset == 0) {
            return Length;
        }

        offset = alignment - offset;
        stream.Seek(offset, SeekOrigin.Current);
        return Length + (int)offset;
    }

    public string GetString(int offset, int length)
        => Encoding.UTF8.GetString(AsSpan().Slice(offset, length));

    public string GetNullTerminatedString(int offset)
    {
        var str = AsSpan()[offset..];
        var size = str.IndexOf((byte)0);
        if (size >= 0) {
            str = str[..size];
        }

        return Encoding.UTF8.GetString(str);
    }

    public (int Offset, int Length) FindString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return (FindString(bytes), bytes.Length);
    }

    public (int Offset, int Length) FindOrAddString(string str, bool preservePosition = false)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return (FindOrAddString(bytes, preservePosition), bytes.Length);
    }

    public int FindString(ReadOnlySpan<byte> str)
    {
        var dataSpan = AsSpan();
        foreach (var offset in StartingOffsets) {
            if (offset + str.Length >= Data.Length) {
                break;
            }

            var strSpan = dataSpan[offset..];
            if (strSpan[..str.Length].SequenceEqual(str) && strSpan[str.Length] == 0) {
                return offset;
            }
        }

        return -1;
    }

    public int FindOrAddString(ReadOnlySpan<byte> str, bool preservePosition = false)
    {
        var existingOffset = FindString(str);
        return existingOffset >= 0
            ? existingOffset
            : AddString(str, preservePosition);
    }

    private int AddString(ReadOnlySpan<byte> str, bool preservePosition = false)
    {
        using var _ = new StreamMovement(Data, preservePosition);
        Data.Seek(0L, SeekOrigin.End);
        var newOffset = (int)Data.Position;
        StartingOffsets.Add(newOffset);
        Data.Write(str);
        Data.WriteByte(0);
        return newOffset;
    }
}
