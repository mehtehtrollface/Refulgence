using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Refulgence.Sm5;

[StructLayout(LayoutKind.Sequential)]
public struct ExtendedOperandToken(uint value) : IEquatable<ExtendedOperandToken>
{
    public uint Value = value;

    public ExtendedOperandType Type
    {
        readonly get => (ExtendedOperandType)(Value & 0x3Fu);
        set => Value = (Value & ~0x3Fu) | ((uint)value & 0x3Fu);
    }

    public OperandModifier Modifier
    {
        readonly get => (OperandModifier)((Value >> 6) & 0xFFu);
        set => Value = (Value & ~0x00003FC0u) | (((uint)value & 0xFFu) << 6);
    }

    public bool Extended
    {
        readonly get => (Value & 0x80000000u) != 0u;
        set => Value = value ? Value | 0x80000000u : Value & ~0x80000000u;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ExtendedOperandToken token && Equals(token);

    public bool Equals(ExtendedOperandToken other)
        => Value.Equals(other.Value);

    public override int GetHashCode()
        => Value.GetHashCode();

    public static ref ExtendedOperandToken Wrap(ref uint value)
        => ref MemoryMarshal.Cast<uint, ExtendedOperandToken>(new(ref value))[0];

    public static bool operator ==(ExtendedOperandToken left, ExtendedOperandToken right)
        => left.Value.Equals(right.Value);

    public static bool operator !=(ExtendedOperandToken left, ExtendedOperandToken right)
        => !left.Value.Equals(right.Value);
}
