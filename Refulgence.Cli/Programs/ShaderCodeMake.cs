using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Refulgence.Cli.IO;
using Refulgence.Text;
using Refulgence.Xiv;

namespace Refulgence.Cli.Programs;

public static class ShaderCodeMake
{
    public static int Run(string inputFileName, string outputFileName)
    {
        var fileBytes = File.ReadAllBytes(inputFileName);
        var magic = MemoryMarshal.Read<InlineByteString<uint>>(fileBytes);
        Shader shader;
        if (magic == "DXBC"u8) {
            shader = Shader.FromDirectX11ShaderBlob(fileBytes);
        } else if (magic == "ShCd"u8) {
            shader = Shader.FromShaderCodeBytes(fileBytes);
        } else {
            throw new InvalidDataException($"Unrecognized magic number {magic}");
        }

        File.WriteAllBytes(outputFileName, shader.ToShaderCodeBytes());

        return 0;
    }
}
