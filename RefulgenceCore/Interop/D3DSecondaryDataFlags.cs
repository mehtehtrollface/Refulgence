namespace Refulgence.Interop;

[Flags]
public enum D3DSecondaryDataFlags : uint
{
    /// <summary>
    ///     Merge unordered access view (UAV) slots in the secondary data.
    /// </summary>
    MergeUavSlots = 0x1,

    /// <summary>
    ///     Preserve template slots in the secondary data.
    /// </summary>
    PreserveTemplateSlots = 0x2,

    /// <summary>
    ///     Require that templates in the secondary data match when the compiler compiles the HLSL code.
    /// </summary>
    RequireTemplateMatch = 0x4,
}
