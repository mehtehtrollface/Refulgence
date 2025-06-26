using System.ComponentModel;

namespace Refulgence.Dxbc;

public static class EnumExtensions
{
    public static ResourceDefinition.ProgramType ToRdefProgramType(this ProgramType programType)
        => programType switch
        {
            ProgramType.PixelShader    => ResourceDefinition.ProgramType.PixelShader,
            ProgramType.VertexShader   => ResourceDefinition.ProgramType.VertexShader,
            ProgramType.GeometryShader => ResourceDefinition.ProgramType.GeometryShader,
            ProgramType.HullShader     => ResourceDefinition.ProgramType.HullShader,
            ProgramType.DomainShader   => ResourceDefinition.ProgramType.DomainShader,
            ProgramType.ComputeShader  => ResourceDefinition.ProgramType.ComputeShader,
            _ => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static string ToAbbreviation(this ProgramType programType)
        => programType switch
        {
            ProgramType.PixelShader    => "ps",
            ProgramType.VertexShader   => "vs",
            ProgramType.GeometryShader => "gs",
            ProgramType.HullShader     => "hs",
            ProgramType.DomainShader   => "ds",
            ProgramType.ComputeShader  => "cs",
            _                          => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
        };

    public static ProgramType ToProgramType(this string abbreviation)
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
