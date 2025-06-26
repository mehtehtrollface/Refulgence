using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Refulgence.Sm5;

[StructLayout(LayoutKind.Sequential)]
public struct OpCodeToken(uint value) : IEquatable<OpCodeToken>
{
    public uint Value = value;

    public OpCodeType Type
    {
        readonly get => (OpCodeType)(Value & 0x7FFu);
        set => Value = (Value & ~0x7FFu) | ((uint)value & 0x7FFu);
    }

    public CustomDataClass CustomDataClass
    {
        readonly get => (CustomDataClass)(Value >> 11);
        set => Value = (Value & 0x7FFu) | ((uint)value << 11);
    }

    public GlobalFlags GlobalFlags
    {
        readonly get => (GlobalFlags)((Value >> 11) & 0x1FFFu);
        set => Value = (Value & ~0x00FFF800u) | (((uint)value & 0x1FFFu) << 11);
    }

    public PrimitiveTopology PrimitiveTopology
    {
        readonly get => (PrimitiveTopology)((Value >> 11) & 0x7Fu);
        set => Value = (Value & ~0x0003F800u) | (((uint)value & 0x7Fu) << 11);
    }

    public Primitive Primitive
    {
        readonly get => (Primitive)((Value >> 11) & 0x3Fu);
        set => Value = (Value & ~0x0001F800u) | (((uint)value & 0x3Fu) << 11);
    }

    public byte ControlPointCount
    {
        readonly get => (byte)((Value >> 11) & 0x3Fu);
        set => Value = (Value & ~0x0001F800u) | ((value & 0x3Fu) << 11);
    }

    public ResourceDimension ResourceDimension
    {
        readonly get => (ResourceDimension)((Value >> 11) & 0x1Fu);
        set => Value = (Value & ~0x0000F800u) | (((uint)value & 0x1Fu) << 11);
    }

    public InterpolationMode InterpolationMode
    {
        readonly get => (InterpolationMode)((Value >> 11) & 0xFu);
        set => Value = (Value & ~0x00007800u) | (((uint)value & 0xFu) << 11);
    }

    public SyncFlags SyncFlags
    {
        readonly get => (SyncFlags)((Value >> 11) & 0xFu);
        set => Value = (Value & ~0x00007800u) | (((uint)value & 0xFu) << 11);
    }

    public SamplerMode SamplerMode
    {
        readonly get => (SamplerMode)((Value >> 11) & 0xFu);
        set => Value = (Value & ~0x00007800u) | (((uint)value & 0xFu) << 11);
    }

    public TessellatorPartitioning TessellatorPartitioning
    {
        readonly get => (TessellatorPartitioning)((Value >> 11) & 0x7u);
        set => Value = (Value & ~0x00003800u) | (((uint)value & 0x7u) << 11);
    }

    public TessellatorOutputPrimitive TessellatorOutputPrimitive
    {
        readonly get => (TessellatorOutputPrimitive)((Value >> 11) & 0x7u);
        set => Value = (Value & ~0x00003800u) | (((uint)value & 0x7u) << 11);
    }

    public TessellatorDomain TessellatorDomain
    {
        readonly get => (TessellatorDomain)((Value >> 11) & 0x3u);
        set => Value = (Value & ~0x00001800u) | (((uint)value & 0x3u) << 11);
    }

    public ResInfoReturnType ResInfoReturnType
    {
        readonly get => (ResInfoReturnType)((Value >> 11) & 0x3u);
        set => Value = (Value & ~0x00001800u) | (((uint)value & 0x3u) << 11);
    }

    public ConstantBufferAccessPattern ConstantBufferAccessPattern
    {
        readonly get => (ConstantBufferAccessPattern)((Value >> 11) & 0x1u);
        set => Value = (Value & ~0x00000800u) | (((uint)value & 0x1u) << 11);
    }

    public bool Saturate
    {
        readonly get => (Value & 0x00002000u) != 0u;
        set => Value = value ? Value | 0x00002000u : Value & ~0x00002000u;
    }

    public byte NumSamples
    {
        readonly get => (byte)((Value >> 16) & 0x7Fu);
        set => Value = (Value & ~0x007F0000u) | ((value & 0x7Fu) << 16);
    }

    public bool GloballyCoherentAccess
    {
        readonly get => (Value & 0x00010000u) != 0u;
        set => Value = value ? Value | 0x00010000u : Value & ~0x00010000u;
    }

    public Test Test
    {
        readonly get => (Test)((Value >> 18) & 0x1u);
        set => Value = (Value & ~0x00040000u) | (((uint)value & 0x1u) << 18);
    }

    public ComponentMask PreciseValues
    {
        readonly get => (ComponentMask)((Value >> 19) & 0xFu);
        set => Value = (Value & ~0x00780000u) | (((uint)value & 0xFu) << 19);
    }

    public bool UavHasOrderPreservingCounter
    {
        readonly get => (Value & 0x00800000u) != 0u;
        set => Value = value ? Value | 0x00800000u : Value & ~0x00800000u;
    }

    public byte Length
    {
        readonly get => (byte)((Value >> 24) & 0x7Fu);
        set => Value = (Value & ~0x7F000000u) | ((value & 0x7Fu) << 24);
    }

    public bool Extended
    {
        readonly get => (Value & 0x80000000u) != 0u;
        set => Value = value ? Value | 0x80000000u : Value & ~0x80000000u;
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
        => obj is OpCodeToken token && Equals(token);

    public readonly bool Equals(OpCodeToken other)
        => Value.Equals(other.Value);

    public readonly override int GetHashCode()
        => Value.GetHashCode();

    public static ref OpCodeToken Wrap(ref uint value)
        => ref MemoryMarshal.Cast<uint, OpCodeToken>(new(ref value))[0];

    public static bool operator ==(OpCodeToken left, OpCodeToken right)
        => left.Value.Equals(right.Value);

    public static bool operator !=(OpCodeToken left, OpCodeToken right)
        => !left.Value.Equals(right.Value);
}
