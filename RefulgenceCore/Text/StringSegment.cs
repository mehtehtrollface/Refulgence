using System.Collections;

namespace Refulgence.Text;

public struct StringSegment(string @string, int offset, int count) : IReadOnlyCollection<char>
{
    public readonly string String = @string;

    public readonly int Offset = offset >= 0 && offset <= @string.Length
        ? offset
        : throw new ArgumentOutOfRangeException(nameof(offset));

    public readonly int Count = count >= 0 && offset + count <= @string.Length
        ? count
        : throw new ArgumentOutOfRangeException(nameof(count));

    int IReadOnlyCollection<char>.Count
        => Count;

    public char this[int index]
        => index >= 0 && index < Count
            ? String[Offset + index]
            : throw new ArgumentOutOfRangeException(nameof(index));

    public StringSegment(string @string) : this(@string, 0, @string.Length)
    {
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IEnumerator<char> GetEnumerator()
        => String.Skip(Offset).Take(Count).GetEnumerator();

    public StringSegment Slice(int offset, int count)
    {
        if (offset < 0 || offset > Count) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (count < 0 || offset + count > Count) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new(String, Offset + offset, count);
    }

    public ReadOnlySpan<char> AsSpan()
        => String.AsSpan().Slice(Offset, Count);

    public override string ToString()
        => String.Substring(Offset, Count);
}
