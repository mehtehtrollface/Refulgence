using System.Runtime.Versioning;

namespace Refulgence.Interop;

[Flags]
[SupportedOSPlatform("windows")]
public enum D3DDisassembleFlags : uint
{
    EnableColorCode            = 1,
    EnableDefaultValuePrints   = 2,
    EnableInstructionNumbering = 4,
    EnableInstructionCycle     = 8,
    DisableDebugInfo           = 16,
    EnableInstructionOffset    = 32,
    InstructionOnly            = 64,
    PrintHexLiterals           = 128,
}
