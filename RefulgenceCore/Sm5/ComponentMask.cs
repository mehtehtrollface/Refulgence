namespace Refulgence.Sm5;

[Flags]
public enum ComponentMask : byte
{
    X = 0x1,
    Y = 0x2,
    Z = 0x4,
    W = 0x8,
}
