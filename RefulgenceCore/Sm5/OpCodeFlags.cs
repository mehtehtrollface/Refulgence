namespace Refulgence.Sm5;

[Flags]
public enum OpCodeFlags : uint
{
    CustomOperands      = 0x1,
    HasTest             = 0x2,
    BlockStart          = 0x4,
    BlockEnd            = 0x8,
    FloatOperation      = 0x10,
    IntOperation        = 0x20,
    UIntOperation       = 0x40,
    BitOperation        = 0x80,
    DoubleOperation     = 0x100,
    MoveOperation       = 0x200,
    ConversionOperation = 0x400,
    TextureOperation    = 0x800,
    MemoryOperation     = 0x1000,
    AtomicOperation     = 0x2000,
    FlowControl         = 0x4000,
    Declaration         = 0x8000,
    DebugOperation      = 0x10000,
    Componentwise       = 0x20000,
}
