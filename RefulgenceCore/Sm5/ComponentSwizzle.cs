using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Refulgence.Sm5;

[StructLayout(LayoutKind.Sequential)]
public struct ComponentSwizzle(byte value) : IEquatable<ComponentSwizzle>
{
    public static readonly ComponentSwizzle NoSwizzle  = new(0xE4);
    public static readonly ComponentSwizzle ReplicateX = new(0x00);
    public static readonly ComponentSwizzle ReplicateY = new(0x55);
    public static readonly ComponentSwizzle ReplicateZ = new(0xAA);
    public static readonly ComponentSwizzle ReplicateW = new(0xFF);

    public byte Value = value;

    public ComponentName X
    {
        readonly get => (ComponentName)((Value >> 0) & 0x3u);
        set => Value = (byte)((Value & ~0x03u) | (((uint)value & 0x3u) << 0));
    }

    public ComponentName Y
    {
        readonly get => (ComponentName)((Value >> 2) & 0x3u);
        set => Value = (byte)((Value & ~0x0Cu) | (((uint)value & 0x3u) << 2));
    }

    public ComponentName Z
    {
        readonly get => (ComponentName)((Value >> 4) & 0x3u);
        set => Value = (byte)((Value & ~0x30u) | (((uint)value & 0x3u) << 4));
    }

    public ComponentName W
    {
        readonly get => (ComponentName)((Value >> 6) & 0x3u);
        set => Value = (byte)((Value & ~0xC0u) | (((uint)value & 0x3u) << 6));
    }

    public ComponentSwizzle(ComponentName x, ComponentName y, ComponentName z, ComponentName w)
        : this((byte)(((uint)x & 0x3u) | (((uint)y & 0x3u) << 2) | (((uint)z & 0x3u) << 4) | (((uint)w & 0x3u) << 6)))
    {
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ComponentSwizzle token && Equals(token);

    public readonly bool Equals(ComponentSwizzle other)
        => Value.Equals(other.Value);

    public readonly override int GetHashCode()
        => Value.GetHashCode();

    public readonly override string ToString()
        => $"{X}{Y}{Z}{W}";

    public static ref ComponentSwizzle Wrap(ref byte value)
        => ref MemoryMarshal.Cast<byte, ComponentSwizzle>(new(ref value))[0];

    public static bool operator ==(ComponentSwizzle left, ComponentSwizzle right)
        => left.Value.Equals(right.Value);

    public static bool operator !=(ComponentSwizzle left, ComponentSwizzle right)
        => !left.Value.Equals(right.Value);
}
