using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Refulgence.Text;

[StructLayout(LayoutKind.Sequential)]
public struct InlineByteString<TBacking>(TBacking value) : IEquatable<InlineByteString<TBacking>>, IEquatable<TBacking>
    where TBacking : unmanaged, IBinaryInteger<TBacking>
{
    public TBacking Value = value;

    public InlineByteString(ReadOnlySpan<byte> value) : this(default(TBacking))
    {
        value.CopyTo(AsBytes(ref this));
    }

    public InlineByteString(string value) : this(default(TBacking))
    {
        Encoding.UTF8.GetBytes(value, AsBytes(ref this));
    }

    public readonly override bool Equals(object? obj)
        => obj is InlineByteString<TBacking> other && Equals(other);

    public readonly bool Equals(InlineByteString<TBacking> other)
        => Value.Equals(other.Value);

    public readonly bool Equals(TBacking other)
        => Value.Equals(other);

    public readonly unsafe bool Equals(ReadOnlySpan<byte> other)
    {
        if (other.Length > sizeof(TBacking)) {
            return false;
        }

        var bytes = AsReadOnlyBytes(in this);
        return other.SequenceEqual(bytes[..other.Length]) && (other.Length == sizeof(TBacking) || bytes[other.Length] == 0);
    }

    public readonly override int GetHashCode()
        => Value.GetHashCode();

    public static ref InlineByteString<TBacking> Wrap(ref TBacking value)
        => ref MemoryMarshal.Cast<TBacking, InlineByteString<TBacking>>(new(ref value))[0];

    public static Span<byte> AsBytes(ref InlineByteString<TBacking> value)
        => MemoryMarshal.AsBytes(new Span<InlineByteString<TBacking>>(ref value));

    public static ReadOnlySpan<byte> AsReadOnlyBytes(in InlineByteString<TBacking> value)
        => MemoryMarshal.AsBytes(new ReadOnlySpan<InlineByteString<TBacking>>(in value));

    public static ReadOnlySpan<byte> GetBytes(in InlineByteString<TBacking> value)
    {
        var bytes = AsReadOnlyBytes(in value);
        var nullPos = bytes.IndexOf((byte)0);

        return nullPos < 0 ? bytes : bytes[..nullPos];
    }

    public readonly override string ToString()
        => Encoding.UTF8.GetString(GetBytes(in this));

    public static bool operator ==(InlineByteString<TBacking> left, InlineByteString<TBacking> right)
        => left.Value.Equals(right.Value);

    public static bool operator ==(InlineByteString<TBacking> left, TBacking right)
        => left.Value.Equals(right);

    public static bool operator ==(TBacking left, InlineByteString<TBacking> right)
        => left.Equals(right.Value);

    public static bool operator ==(InlineByteString<TBacking> left, ReadOnlySpan<byte> right)
        => left.Equals(right);

    public static bool operator ==(ReadOnlySpan<byte> left, InlineByteString<TBacking> right)
        => right.Equals(left);

    public static bool operator !=(InlineByteString<TBacking> left, InlineByteString<TBacking> right)
        => !left.Value.Equals(right.Value);

    public static bool operator !=(InlineByteString<TBacking> left, TBacking right)
        => left.Value.Equals(right);

    public static bool operator !=(TBacking left, InlineByteString<TBacking> right)
        => !left.Equals(right.Value);

    public static bool operator !=(InlineByteString<TBacking> left, ReadOnlySpan<byte> right)
        => !left.Equals(right);

    public static bool operator !=(ReadOnlySpan<byte> left, InlineByteString<TBacking> right)
        => !right.Equals(left);
}
