using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Refulgence.Sm5;

[StructLayout(LayoutKind.Sequential)]
public struct ExtendedOpCodeToken(uint value) : IEquatable<ExtendedOpCodeToken>
{
    public uint Value = value;

    public ExtendedOpCodeType Type
    {
        readonly get => (ExtendedOpCodeType)(Value & 0x3Fu);
        set => Value = (Value & ~0x3Fu) | ((uint)value & 0x3Fu);
    }

    public sbyte OffsetU
    {
        readonly get => (sbyte)(unchecked((sbyte)(Value >> 5)) >> 4);
        set => Value = (Value & ~0x1E00u) | ((unchecked((uint)value) & 0xF) << 9);
    }

    public sbyte OffsetV
    {
        readonly get => (sbyte)(unchecked((sbyte)(Value >> 9)) >> 4);
        set => Value = (Value & ~0x1E000u) | ((unchecked((uint)value) & 0xF) << 13);
    }

    public sbyte OffsetW
    {
        readonly get => (sbyte)(unchecked((sbyte)(Value >> 13)) >> 4);
        set => Value = (Value & ~0x1E0000u) | ((unchecked((uint)value) & 0xF) << 17);
    }

    public ResourceDimension ResourceDimension
    {
        readonly get => (ResourceDimension)((Value >> 6) & 0x1Fu);
        set => Value = (Value & ~0x000007C0u) | (((uint)value & 0x1Fu) << 6);
    }

    public ushort ResourceDimensionStructureStride
    {
        readonly get => (ushort)((Value >> 11) & 0xFFFu);
        set => Value = (Value & ~0x007FF800u) | ((value & 0xFFFu) << 11);
    }

    public ResourceReturnType ResourceReturnTypeX
    {
        readonly get => (ResourceReturnType)((Value >> 6) & 0xFu);
        set => Value = (Value & ~0x000003C0u) | (((uint)value & 0xFu) << 6);
    }

    public ResourceReturnType ResourceReturnTypeY
    {
        readonly get => (ResourceReturnType)((Value >> 10) & 0xFu);
        set => Value = (Value & ~0x00003C00u) | (((uint)value & 0xFu) << 10);
    }

    public ResourceReturnType ResourceReturnTypeZ
    {
        readonly get => (ResourceReturnType)((Value >> 14) & 0xFu);
        set => Value = (Value & ~0x0003C000u) | (((uint)value & 0xFu) << 14);
    }

    public ResourceReturnType ResourceReturnTypeW
    {
        readonly get => (ResourceReturnType)((Value >> 18) & 0xFu);
        set => Value = (Value & ~0x003C0000u) | (((uint)value & 0xFu) << 18);
    }

    public bool Extended
    {
        readonly get => (Value & 0x80000000u) != 0u;
        set => Value = value ? Value | 0x80000000u : Value & ~0x80000000u;
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ExtendedOpCodeToken token && Equals(token);

    public readonly bool Equals(ExtendedOpCodeToken other)
        => Value.Equals(other.Value);

    public readonly override int GetHashCode()
        => Value.GetHashCode();

    public static ref ExtendedOpCodeToken Wrap(ref uint value)
        => ref MemoryMarshal.Cast<uint, ExtendedOpCodeToken>(new(ref value))[0];

    public static bool operator ==(ExtendedOpCodeToken left, ExtendedOpCodeToken right)
        => left.Value.Equals(right.Value);

    public static bool operator !=(ExtendedOpCodeToken left, ExtendedOpCodeToken right)
        => !left.Value.Equals(right.Value);
}
