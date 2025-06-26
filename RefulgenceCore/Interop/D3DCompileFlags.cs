using System.Runtime.Versioning;

namespace Refulgence.Interop;

[Flags]
public enum D3DCompileFlags : uint
{
    /// <summary>
    ///     Enable debugging information. (/Zi)
    /// </summary>
    Debug = 0x1,

    /// <summary>
    ///     Disable validation. (/Vd)
    /// </summary>
    SkipValidation = 0x2,

    /// <summary>
    ///     Disable optimizations. (/Od) /Od implies /Gfp, though output may not be identical to /Od /Gfp.
    /// </summary>
    SkipOptimization = 0x4,

    /// <summary>
    ///     Pack matrices in row-major order. (/Zpr)
    /// </summary>
    PackMatrixRowMajor = 0x8,

    /// <summary>
    ///     Pack matrices in column-major order. (/Zpc)
    /// </summary>
    PackMatrixColumnMajor = 0x10,

    /// <summary>
    ///     Force partial precision. (/Gpp)
    /// </summary>
    PartialPrecision = 0x20,
    ForceVsSoftwareNoOpt = 0x40,
    ForcePsSoftwareNoOpt = 0x80,

    /// <summary>
    ///     Disable preshaders (deprecated). (/Op)
    /// </summary>
    NoPreshader = 0x100,

    /// <summary>
    ///     Avoid flow control constructs. (/Gfa)
    /// </summary>
    AvoidFlowControl = 0x200,

    /// <summary>
    ///     Prefer flow control constructs. (/Gfp)
    /// </summary>
    PreferFlowControl = 0x400,

    /// <summary>
    ///     Enable strict mode. (/Ges)
    /// </summary>
    EnableStrictness = 0x800,

    /// <summary>
    ///     Enable backward compatibility mode. (/Gec)
    /// </summary>
    EnableBackwardsCompatibility = 0x1000,

    /// <summary>
    ///     Force IEEE strictness. (/Gis)
    /// </summary>
    IeeeStrictness = 0x2000,

    /// <summary>
    ///     /O0
    /// </summary>
    OptimizationLevel0 = 0x4000,

    /// <summary>
    ///     /O3
    /// </summary>
    OptimizationLevel3 = 0x8000,

    /// <summary>
    ///     Treat warnings as errors. (/WX)
    /// </summary>
    WarningsAreErrors = 0x4_0000,

    /// <summary>
    ///     Assume that UAVs/SRVs may alias for cs_5_0+. (/res_may_alias)
    /// </summary>
    ResourcesMayAlias = 0x8_0000,

    /// <summary>
    ///     Enables unbounded descriptor tables. New for Direct3D 12. (/enable_unbounded_descriptor_tables)
    /// </summary>
    EnableUnboundedDescriptorTables = 0x10_0000,

    /// <summary>
    ///     Enable aggressive flattening in SM5.1+. New for Direct3D 12. (/all_resources_bound)
    /// </summary>
    AllResourcesBound = 0x20_0000,

    /// <summary>
    ///     /Zss
    /// </summary>
    DebugNameForSource = 0x40_0000,

    /// <summary>
    ///     /Zsb
    /// </summary>
    DebugNameForBinary = 0x80_0000,
}
