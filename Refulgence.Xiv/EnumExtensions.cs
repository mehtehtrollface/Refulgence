using System.ComponentModel;
using Refulgence.Dxbc.ResourceDefinition;

namespace Refulgence.Xiv;

public static class EnumExtensions
{
    public static bool IsCompatibleWith(this ResourceDefinitionDxPart rdef, GraphicsPlatform platform)
        => rdef.MajorVersion switch
        {
            3 => platform == GraphicsPlatform.DirectX9,
            5 => platform == GraphicsPlatform.DirectX11,
            _ => false,
        };

    public static ProgramType ToXivProgramType(this Dxbc.ProgramType programType)
        => programType switch
        {
            Dxbc.ProgramType.PixelShader    => ProgramType.PixelShader,
            Dxbc.ProgramType.VertexShader   => ProgramType.VertexShader,
            Dxbc.ProgramType.GeometryShader => ProgramType.GeometryShader,
            Dxbc.ProgramType.HullShader     => ProgramType.HullShader,
            Dxbc.ProgramType.DomainShader   => ProgramType.DomainShader,
            Dxbc.ProgramType.ComputeShader  => ProgramType.ComputeShader,
            _                               => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static ProgramType ToXivProgramType(this Dxbc.ResourceDefinition.ProgramType programType)
        => programType switch
        {
            Dxbc.ResourceDefinition.ProgramType.ComputeShader => ProgramType.ComputeShader,
            Dxbc.ResourceDefinition.ProgramType.DomainShader => ProgramType.DomainShader,
            Dxbc.ResourceDefinition.ProgramType.GeometryShader => ProgramType.GeometryShader,
            Dxbc.ResourceDefinition.ProgramType.HullShader => ProgramType.HullShader,
            Dxbc.ResourceDefinition.ProgramType.VertexShader => ProgramType.VertexShader,
            Dxbc.ResourceDefinition.ProgramType.PixelShader => ProgramType.PixelShader,
            _ => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static Dxbc.ProgramType ToDxbcProgramType(this ProgramType programType)
        => programType switch
        {
            ProgramType.VertexShader   => Dxbc.ProgramType.VertexShader,
            ProgramType.PixelShader    => Dxbc.ProgramType.PixelShader,
            ProgramType.GeometryShader => Dxbc.ProgramType.GeometryShader,
            ProgramType.ComputeShader  => Dxbc.ProgramType.ComputeShader,
            ProgramType.HullShader     => Dxbc.ProgramType.HullShader,
            ProgramType.DomainShader   => Dxbc.ProgramType.DomainShader,
            _                          => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static Dxbc.ResourceDefinition.ProgramType ToRdefProgramType(this ProgramType programType)
        => programType switch
        {
            ProgramType.VertexShader   => Dxbc.ResourceDefinition.ProgramType.VertexShader,
            ProgramType.PixelShader    => Dxbc.ResourceDefinition.ProgramType.PixelShader,
            ProgramType.GeometryShader => Dxbc.ResourceDefinition.ProgramType.GeometryShader,
            ProgramType.ComputeShader  => Dxbc.ResourceDefinition.ProgramType.ComputeShader,
            ProgramType.HullShader     => Dxbc.ResourceDefinition.ProgramType.HullShader,
            ProgramType.DomainShader   => Dxbc.ResourceDefinition.ProgramType.DomainShader,
            _                          => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static string ToAbbreviation(this ProgramType programType)
        => programType switch
        {
            ProgramType.VertexShader   => "vs",
            ProgramType.PixelShader    => "ps",
            ProgramType.GeometryShader => "gs",
            ProgramType.ComputeShader  => "cs",
            ProgramType.HullShader     => "hs",
            ProgramType.DomainShader   => "ds",
            _                          => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static ProgramType ToXivProgramType(this string abbreviation)
        => abbreviation.ToLowerInvariant() switch
        {
            "cs" => ProgramType.ComputeShader,
            "ds" => ProgramType.DomainShader,
            "gs" => ProgramType.GeometryShader,
            "hs" => ProgramType.HullShader,
            "ps" => ProgramType.PixelShader,
            "vs" => ProgramType.VertexShader,
            _    => throw new InvalidEnumArgumentException($"Invalid program type abbreviation {abbreviation}"),
        };
}
