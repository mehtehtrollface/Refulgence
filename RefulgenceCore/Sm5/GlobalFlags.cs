namespace Refulgence.Sm5;

[Flags]
public enum GlobalFlags : ushort
{
    RefactoringAllowed            = 0x1,
    EnableDoublePrecisionFloatOps = 0x2,
    ForceEarlyDepthStencil        = 0x4,
    EnableRawAndStructuredBuffers = 0x8,
}
