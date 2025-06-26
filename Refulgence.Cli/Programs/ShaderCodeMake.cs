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
        using var mmio = MmioMemoryManager.CreateFromFile(inputFileName, access: MemoryMappedFileAccess.Read);
        var mmioSpan = (ReadOnlySpan<byte>)mmio.GetSpan();
        var magic = MemoryMarshal.Read<InlineByteString<uint>>(mmioSpan);
        Shader shader;
        if (magic == "DXBC"u8) {
            shader = Shader.FromDirectX11ShaderBlob(mmioSpan.ToArray());
        } else if (magic == "ShCd"u8) {
            shader = Shader.FromShaderCodeBytes(mmioSpan);
        } else {
            throw new InvalidDataException($"Unrecognized magic number {magic}");
        }

        File.WriteAllBytes(outputFileName, shader.ToShaderCodeBytes());

        return 0;
    }
}
