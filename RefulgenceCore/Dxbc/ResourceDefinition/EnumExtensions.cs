using System.ComponentModel;

namespace Refulgence.Dxbc.ResourceDefinition;

public static class EnumExtensions
{
    public static Dxbc.ProgramType ToShdrProgramType(this ProgramType programType)
        => programType switch
        {
            ProgramType.PixelShader    => Dxbc.ProgramType.PixelShader,
            ProgramType.VertexShader   => Dxbc.ProgramType.VertexShader,
            ProgramType.GeometryShader => Dxbc.ProgramType.GeometryShader,
            ProgramType.HullShader     => Dxbc.ProgramType.HullShader,
            ProgramType.DomainShader   => Dxbc.ProgramType.DomainShader,
            ProgramType.ComputeShader  => Dxbc.ProgramType.ComputeShader,
            _                          => throw new InvalidEnumArgumentException($"Invalid program type {programType}"),
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

    public static ProgramType ToRdefProgramType(this string abbreviation)
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

    public static string ToTableString(this ShaderInputType inputType)
        => inputType switch
        {
            ShaderInputType.CBuffer                    => "cbuffer",
            ShaderInputType.TBuffer                    => "tbuffer",
            ShaderInputType.Texture                    => "texture",
            ShaderInputType.Sampler                    => "sampler",
            ShaderInputType.UavRWTyped                 => "UAV",
            ShaderInputType.Structured                 => "texture",
            ShaderInputType.UavRWStructured            => "UAV",
            ShaderInputType.ByteAddress                => "texture",
            ShaderInputType.UavRWByteAddress           => "UAV",
            ShaderInputType.UavAppendStructured        => "UAV",
            ShaderInputType.UavConsumeStructured       => "UAV",
            ShaderInputType.UavRWStructuredWithCounter => "UAV",
            _                                          => "void",
        };

    public static string ToTableString(this ResourceReturnType returnType, ShaderInputFlags inputFlags)
    {
        var components = (inputFlags & (ShaderInputFlags.TextureComponent0 | ShaderInputFlags.TextureComponent1)) switch
        {
            ShaderInputFlags.TextureComponent0                                      => "2",
            ShaderInputFlags.TextureComponent1                                      => "3",
            ShaderInputFlags.TextureComponent0 | ShaderInputFlags.TextureComponent1 => "4",
            _                                                                       => string.Empty,
        };

        return returnType switch
        {
            ResourceReturnType.UNorm     => $"unorm{components}",
            ResourceReturnType.SNorm     => $"snorm{components}",
            ResourceReturnType.SInt      => $"sint{components}",
            ResourceReturnType.UInt      => $"uint{components}",
            ResourceReturnType.Float     => $"float{components}",
            ResourceReturnType.Mixed     => $"mixed{components}",
            ResourceReturnType.Double    => $"double{components}",
            ResourceReturnType.Continued => "<continued>",
            _                            => "NA",
        };
    }

    public static string ToTableString(this ShaderResourceViewDimension viewDimension, ShaderInputType inputType, uint numSamples)
        => inputType switch
        {
            ShaderInputType.Texture or ShaderInputType.UavRWTyped => viewDimension switch
            {
                ShaderResourceViewDimension.Buffer           => "buf",
                ShaderResourceViewDimension.Texture1D        => "1d",
                ShaderResourceViewDimension.Texture1DArray   => "1darray",
                ShaderResourceViewDimension.Texture2D        => "2d",
                ShaderResourceViewDimension.Texture2DArray   => "2darray",
                ShaderResourceViewDimension.Texture2DMS      => $"2dMS{numSamples}",
                ShaderResourceViewDimension.Texture2DMSArray => $"2darrayMS{numSamples}",
                ShaderResourceViewDimension.Texture3D        => "3d",
                ShaderResourceViewDimension.TextureCube      => "cube",
                ShaderResourceViewDimension.TextureCubeArray => "cubearray",
                ShaderResourceViewDimension.BufferEx         => "buf",
                _                                            => "NA",
            },
            ShaderInputType.Structured                 => "r/o",
            ShaderInputType.UavRWStructured            => "r/w",
            ShaderInputType.ByteAddress                => "r/o",
            ShaderInputType.UavRWByteAddress           => "r/w",
            ShaderInputType.UavAppendStructured        => "append",
            ShaderInputType.UavConsumeStructured       => "consume",
            ShaderInputType.UavRWStructuredWithCounter => "r/w+cnt",
            _                                          => "NA",
        };

    public static string ToBindPointString(this ShaderInputType inputType, uint bindPoint)
        => inputType switch
        {
            ShaderInputType.CBuffer => $"cb{bindPoint}",
            ShaderInputType.TBuffer => $"t{bindPoint}",
            ShaderInputType.Texture => $"t{bindPoint}",
            ShaderInputType.Sampler => $"s{bindPoint}",
            ShaderInputType.UavRWTyped => $"u{bindPoint}",
            ShaderInputType.Structured => $"t{bindPoint}",
            ShaderInputType.UavRWStructured => $"u{bindPoint}",
            ShaderInputType.ByteAddress => $"t{bindPoint}",
            ShaderInputType.UavRWByteAddress => $"u{bindPoint}",
            ShaderInputType.UavAppendStructured => $"u{bindPoint}",
            ShaderInputType.UavConsumeStructured => $"u{bindPoint}",
            ShaderInputType.UavRWStructuredWithCounter => $"u{bindPoint}",
            _ => throw new InvalidEnumArgumentException($"Invalid input type {inputType}"),
        };

    public static string ToDeclarationKeyword(this ConstantBufferType bufferType)
        => bufferType switch
        {
            ConstantBufferType.CBuffer           => "cbuffer",
            ConstantBufferType.TBuffer           => "tbuffer",
            ConstantBufferType.InterfacePointers => "interfaces",
            ConstantBufferType.ResourceBindInfo  => "Resource bind info for",
            _                                    => throw new InvalidEnumArgumentException($"Invalid buffer type {bufferType}"),
        };
}
