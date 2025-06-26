using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Refulgence.Sm5;

[StructLayout(LayoutKind.Sequential)]
public struct OperandToken(uint value) : IEquatable<OperandToken>
{
    public uint Value = value;

    public OperandNumComponents NumComponents
    {
        readonly get => (OperandNumComponents)(Value & 0x3u);
        set => Value = (Value & ~0x3u) | ((uint)value & 0x3u);
    }

    public ComponentSelectionMode ComponentSelectionMode
    {
        readonly get => (ComponentSelectionMode)((Value >> 2) & 0x3u);
        set => Value = (Value & ~0x0000000Cu) | (((uint)value & 0x3u) << 2);
    }

    public ComponentMask Mask
    {
        readonly get => (ComponentMask)((Value >> 4) & 0xFu);
        set => Value = (Value & ~0x000000F0u) | (((uint)value & 0xFu) << 4);
    }

    public ComponentSwizzle Swizzle
    {
        readonly get => new(unchecked((byte)(Value >> 4)));
        set => Value = (Value & ~0x00000FF0u) | ((uint)value.Value << 4);
    }

    public ComponentName Component
    {
        readonly get => (ComponentName)((Value >> 4) & 0x3u);
        set => Value = (Value & ~0x00000030u) | (((uint)value & 0x3u) << 4);
    }

    public OperandType Type
    {
        readonly get => (OperandType)((Value >> 12) & 0xFFu);
        set => Value = (Value & ~0x000FF000u) | (((uint)value & 0xFFu) << 12);
    }

    public byte IndexDimensions
    {
        readonly get => (byte)((Value >> 20) & 0x3u);
        set => Value = (Value & ~0x00300000u) | ((value & 0x3u) << 20);
    }

    public OperandIndexRepresentation Index0Representation
    {
        readonly get => (OperandIndexRepresentation)((Value >> 22) & 0x7u);
        set => Value = (Value & ~0x01C00000u) | (((uint)value & 0x7u) << 22);
    }

    public OperandIndexRepresentation Index1Representation
    {
        readonly get => (OperandIndexRepresentation)((Value >> 25) & 0x7u);
        set => Value = (Value & ~0x0E000000u) | (((uint)value & 0x7u) << 25);
    }

    public OperandIndexRepresentation Index2Representation
    {
        readonly get => (OperandIndexRepresentation)((Value >> 28) & 0x7u);
        set => Value = (Value & ~0x70000000u) | (((uint)value & 0x7u) << 28);
    }

    public bool Extended
    {
        readonly get => (Value & 0x80000000u) != 0u;
        set => Value = value ? Value | 0x80000000u : Value & ~0x80000000u;
    }

    public readonly string ComponentsToString()
        => NumComponents is OperandNumComponents.Four
            ? ComponentSelectionMode switch
            {
                ComponentSelectionMode.Mask    => "." + Mask.ToCompactString(),
                ComponentSelectionMode.Swizzle => "." + Swizzle.ToString().ToLowerInvariant(),
                ComponentSelectionMode.Select1 => "." + Component.ToLowerString(),
                _                              => string.Empty,
            }
            : string.Empty;

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is OperandToken token && Equals(token);

    public bool Equals(OperandToken other)
        => Value.Equals(other.Value);

    public override int GetHashCode()
        => Value.GetHashCode();

    public static ref OperandToken Wrap(ref uint value)
        => ref MemoryMarshal.Cast<uint, OperandToken>(new(ref value))[0];

    public static bool operator ==(OperandToken left, OperandToken right)
        => left.Value.Equals(right.Value);

    public static bool operator !=(OperandToken left, OperandToken right)
        => !left.Value.Equals(right.Value);
}
