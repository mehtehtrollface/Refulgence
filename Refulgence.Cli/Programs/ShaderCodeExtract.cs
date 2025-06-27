using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Refulgence.Cli.IO;
using Refulgence.Text;
using Refulgence.Xiv;

namespace Refulgence.Cli.Programs;

public static class ShaderCodeExtract
{
    public static int Run(string inputFileName, string outputFileName)
    {
        var fileBytes = File.ReadAllBytes(inputFileName);
        var magic = MemoryMarshal.Read<InlineByteString<uint>>(fileBytes);
        byte[] shader;
        if (magic == "DXBC"u8) {
            shader = fileBytes;
        } else if (magic == "ShCd"u8) {
            shader = Shader.FromShaderCodeBytes(fileBytes).ShaderBlob;
        } else {
            throw new InvalidDataException($"Unrecognized magic number {magic}");
        }

        File.WriteAllBytes(outputFileName, shader);

        return 0;
    }
}
