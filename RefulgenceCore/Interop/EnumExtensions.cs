namespace Refulgence.Interop;

public static class EnumExtensions
{
    public static IEnumerable<string> ToCompilerSwitches(this D3DCompileFlags flags)
    {
        if (flags.HasFlag(D3DCompileFlags.Debug)) {
            yield return "Zi";
        }

        if (flags.HasFlag(D3DCompileFlags.SkipValidation)) {
            yield return "Vd";
        }

        if (flags.HasFlag(D3DCompileFlags.SkipOptimization)) {
            yield return "Od";
        }

        if (flags.HasFlag(D3DCompileFlags.PackMatrixRowMajor)) {
            yield return "Zpr";
        }

        if (flags.HasFlag(D3DCompileFlags.PackMatrixColumnMajor)) {
            yield return "Zpc";
        }

        if (flags.HasFlag(D3DCompileFlags.PartialPrecision)) {
            yield return "Gpp";
        }

        if (flags.HasFlag(D3DCompileFlags.NoPreshader)) {
            yield return "Op";
        }

        if (flags.HasFlag(D3DCompileFlags.AvoidFlowControl)) {
            yield return "Gfa";
        }

        if (flags.HasFlag(D3DCompileFlags.PreferFlowControl)) {
            yield return "Gfp";
        }

        if (flags.HasFlag(D3DCompileFlags.EnableStrictness)) {
            yield return "Ges";
        }

        if (flags.HasFlag(D3DCompileFlags.EnableBackwardsCompatibility)) {
            yield return "Gec";
        }

        if (flags.HasFlag(D3DCompileFlags.IeeeStrictness)) {
            yield return "Gis";
        }

        if (flags.HasFlag(D3DCompileFlags.OptimizationLevel0)) {
            if (flags.HasFlag(D3DCompileFlags.OptimizationLevel3)) {
                yield return "O2";
            } else {
                yield return "O0";
            }
        } else if (flags.HasFlag(D3DCompileFlags.OptimizationLevel3)) {
            yield return "O3";
        }

        if (flags.HasFlag(D3DCompileFlags.WarningsAreErrors)) {
            yield return "WX";
        }

        if (flags.HasFlag(D3DCompileFlags.ResourcesMayAlias)) {
            yield return "res_may_alias";
        }

        if (flags.HasFlag(D3DCompileFlags.EnableUnboundedDescriptorTables)) {
            yield return "enable_unbounded_descriptor_tables";
        }

        if (flags.HasFlag(D3DCompileFlags.AllResourcesBound)) {
            yield return "all_resources_bound";
        }

        if (flags.HasFlag(D3DCompileFlags.DebugNameForSource)) {
            yield return "Zss";
        }

        if (flags.HasFlag(D3DCompileFlags.DebugNameForBinary)) {
            yield return "Zsb";
        }
    }
}
