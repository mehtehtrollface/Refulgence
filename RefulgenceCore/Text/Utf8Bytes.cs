using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Refulgence.Text;

public readonly struct Utf8Bytes(byte[] bytes)
{
    public readonly byte[] Bytes = bytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToByteArray()
        => Bytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
        => Bytes;

    public override string ToString()
        => Encoding.UTF8.GetString(Bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Utf8Bytes FromString(string str)
        => new(Encoding.UTF8.GetBytes(str));

    [ExcludeFromCodeCoverage]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte[](Utf8Bytes bytes)
        => bytes.Bytes;

    [ExcludeFromCodeCoverage]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(Utf8Bytes bytes)
        => bytes.Bytes;

    [ExcludeFromCodeCoverage]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Utf8Bytes bytes)
        => Encoding.UTF8.GetString(bytes.Bytes);
}
