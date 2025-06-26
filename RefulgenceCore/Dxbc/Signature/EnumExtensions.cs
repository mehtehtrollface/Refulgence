using System.ComponentModel;

namespace Refulgence.Dxbc.Signature;

public static class EnumExtensions
{
    public static string ToTableString(this SystemValueType systemValueType)
        => systemValueType switch
        {
            SystemValueType.Position                   => "POS",
            SystemValueType.ClipDistance               => "CLIPDST",
            SystemValueType.CullDistance               => "CULLDST",
            SystemValueType.RenderTargetArrayIndex     => "RTINDEX",
            SystemValueType.ViewportArrayIndex         => "VPINDEX",
            SystemValueType.VertexID                   => "VERTID",
            SystemValueType.InstanceID                 => "INSTID",
            SystemValueType.IsFrontFace                => "FFACE",
            SystemValueType.SampleIndex                => "SAMPLE",
            SystemValueType.FinalQuadEdgeTessFactor    => "QUADEDGE",
            SystemValueType.FinalQuadInsideTessFactor  => "QUADINT",
            SystemValueType.FinalTriEdgeTessFactor     => "TRIEDGE",
            SystemValueType.FinalTriInsideTessFactor   => "TRIINT",
            SystemValueType.FinalLineDetailTessFactor  => "LINEDET",
            SystemValueType.FinalLineDensityTessFactor => "LINEDEN",
            SystemValueType.Target                     => "TARGET",
            SystemValueType.Depth                      => "DEPTH",
            SystemValueType.Coverage                   => "COVERAGE",
            SystemValueType.DepthGreaterEqual          => "DEPTHGE",
            SystemValueType.DepthLessEqual             => "DEPTHLE",
            SystemValueType.StencilRef                 => "STENCILREF",
            SystemValueType.InnerCoverage              => "INNERCOV",
            _                                          => "NONE",
        };

    public static string ToTableString(this RegisterComponentType componentType)
        => componentType switch
        {
            RegisterComponentType.Unknown => "unknown",
            RegisterComponentType.UInt32  => "uint",
            RegisterComponentType.SInt32  => "int",
            RegisterComponentType.Float32 => "float",
            RegisterComponentType.UInt16  => "ushort",
            RegisterComponentType.SInt16  => "short",
            RegisterComponentType.Float16 => "half",
            RegisterComponentType.UInt64  => "ulong",
            RegisterComponentType.SInt64  => "long",
            RegisterComponentType.Float64 => "double",
            _                             => throw new InvalidEnumArgumentException($"Invalid component type {componentType}"),
        };
}
