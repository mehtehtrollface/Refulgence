using System.Buffers;
using System.IO.MemoryMappedFiles;
using Refulgence.Cli.IO;
using Refulgence.Xiv;
using Refulgence.Xiv.ShaderPackages;

namespace Refulgence.Cli.Programs;

public static class ShaderPackageExtract
{
    private static readonly SearchValues<char> Digits = SearchValues.Create("0123456789");

    public static int Run(string inputFileName, ReadOnlySpan<string> shaderIds, bool asShaderCode)
    {
        ShaderPackage shpk;
        using (var mmio = MmioMemoryManager.CreateFromFile(inputFileName, access: MemoryMappedFileAccess.Read)) {
            shpk = ShaderPackage.FromShaderPackageBytes(mmio.GetSpan());
        }

        var outputDirectory = Path.ChangeExtension(inputFileName, null);
        Directory.CreateDirectory(outputDirectory);

        if (shaderIds.Length == 0) {
            foreach (var (programType, shaders) in shpk.GetShaders()) {
                for (var i = 0; i < shaders.Count; ++i) {
                    ExtractShader(outputDirectory, $"{programType.ToAbbreviation()}{i}", shaders[i], asShaderCode);
                }
            }
        } else {
            foreach (var shaderId in shaderIds) {
                var (programType, index) = ParseShaderId(shaderId);
                ExtractShader(outputDirectory, shaderId, shpk.GetShadersByProgramType(programType)[(int)index], asShaderCode);
            }
        }

        return 0;
    }

    private static void ExtractShader(string outputDirectory, string baseName, Shader shader, bool asShaderCode)
    {
        if (asShaderCode) {
            File.WriteAllBytes(Path.Combine(outputDirectory, baseName + ".shcd"), shader.ToShaderCodeBytes());
        } else {
            File.WriteAllBytes(Path.Combine(outputDirectory, baseName + ".dxbc"), shader.ShaderBlob);
        }

        Console.Error.WriteLine($"Extracted shader {baseName}");
    }

    public static (ProgramType ProgramType, uint Index) ParseShaderId(string shaderId)
    {
        var firstDigit = shaderId.AsSpan().IndexOfAny(Digits);
        return (shaderId[..firstDigit].ToLowerInvariant().ToXivProgramType(), uint.Parse(shaderId.AsSpan(firstDigit)));
    }
}
