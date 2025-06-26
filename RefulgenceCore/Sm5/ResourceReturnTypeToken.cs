using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Refulgence.Sm5;

[StructLayout(LayoutKind.Sequential)]
public struct ResourceReturnTypeToken(uint value) : IEquatable<ResourceReturnTypeToken>
{
    public uint Value = value;

    public ResourceReturnType ResourceReturnTypeX
    {
        readonly get => (ResourceReturnType)((Value >> 0) & 0xFu);
        set => Value = (Value & ~0x0000000Fu) | (((uint)value & 0xFu) << 0);
    }

    public ResourceReturnType ResourceReturnTypeY
    {
        readonly get => (ResourceReturnType)((Value >> 4) & 0xFu);
        set => Value = (Value & ~0x000000F0u) | (((uint)value & 0xFu) << 4);
    }

    public ResourceReturnType ResourceReturnTypeZ
    {
        readonly get => (ResourceReturnType)((Value >> 8) & 0xFu);
        set => Value = (Value & ~0x00000F00u) | (((uint)value & 0xFu) << 8);
    }

    public ResourceReturnType ResourceReturnTypeW
    {
        readonly get => (ResourceReturnType)((Value >> 12) & 0xFu);
        set => Value = (Value & ~0x0000F000u) | (((uint)value & 0xFu) << 12);
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ResourceReturnTypeToken token && Equals(token);

    public readonly bool Equals(ResourceReturnTypeToken other)
        => Value.Equals(other.Value);

    public readonly override int GetHashCode()
        => Value.GetHashCode();

    public static ref ResourceReturnTypeToken Wrap(ref uint value)
        => ref MemoryMarshal.Cast<uint, ResourceReturnTypeToken>(new(ref value))[0];

    public static bool operator ==(ResourceReturnTypeToken left, ResourceReturnTypeToken right)
        => left.Value.Equals(right.Value);

    public static bool operator !=(ResourceReturnTypeToken left, ResourceReturnTypeToken right)
        => !left.Value.Equals(right.Value);
}
