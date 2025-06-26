using System.Runtime.InteropServices;
using Refulgence.Collections;

namespace Refulgence.Xiv.IO;

internal static partial class ShaderReaderWriter
{
    public static int GetShaderExtraHeaderSize(GraphicsPlatform platform, ProgramType type)
        => type switch
        {
            ProgramType.VertexShader => platform switch
            {
                GraphicsPlatform.DirectX9  => 4,
                GraphicsPlatform.DirectX11 => 8,
                _ => throw new NotImplementedException(
                    $"{nameof(GetShaderExtraHeaderSize)}: unimplemented graphics platform {platform}"
                ),
            },
            _ => 0,
        };

    private static void ReadCode3(Shader result, ref Reader reader)
        => DoReadCode3(result, ref reader, in reader.Header.ReadRef<ShaderHeader3>());

    private static void DoReadCode3(Shader result, ref Reader reader, in ShaderHeader3 header)
    {
        var headerSize = GetShaderExtraHeaderSize(result.GraphicsPlatform, result.ProgramType);
        var rawBlob = reader.Blobs.Slice((int)header.BlobOffset, (int)header.BlobSize);

        result.AdditionalHeader = rawBlob[..headerSize].ToArray();
        result.ShaderBlob = rawBlob[headerSize..].ToArray();

        ReadResources(result.ConstantBuffers, ref reader, header.ConstantCount);
        ReadResources(result.Samplers,  ref reader, header.SamplerCount);
    }

    private static void ReadCode5(Shader result, ref Reader reader)
        => DoReadCode5(result, ref reader, in reader.Header.ReadRef<ShaderHeader5>());

    private static void DoReadCode5(Shader result, ref Reader reader, in ShaderHeader5 header)
    {
        DoReadCode3(result, ref reader, in header.Super);
        ReadResources(result.Uavs,     ref reader, header.UavCount);
        ReadResources(result.Textures, ref reader, header.TextureCount);
    }

    private static void ReadCode6(Shader result, ref Reader reader, uint expectedUnk)
        => DoReadCode6(result, ref reader, in reader.Header.ReadRef<ShaderHeader6>(), expectedUnk);

    private static void DoReadCode6(Shader result, ref Reader reader, in ShaderHeader6 header, uint expectedUnk)
    {
        DoReadCode5(result, ref reader, in header.Super);
        if (header.Unk != expectedUnk) {
            throw new InvalidDataException($"Unrecognized Shader.Unk value {header.Unk:X}, expected {expectedUnk:X}");
        }
    }

    private static void ReadResources(IndexedList<Name, ShaderResource> result, ref Reader reader, int count)
    {
        result.EnsureCapacity(result.Count + count);
        for (var i = 0; i < count; ++i) {
            var resource = reader.Header.Read<Resource>();

            var name = new Name(
                resource.Crc32,
                reader.Strings.ReadString((int)resource.NameOffset, resource.NameSize)
            );

            result.Add(new(name, resource.Type, resource.Slot, resource.Size));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderHeader3
    {
        public uint   BlobOffset;
        public uint   BlobSize;
        public ushort ConstantCount;
        public ushort SamplerCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderHeader5
    {
        public ShaderHeader3 Super;
        public ushort        UavCount;
        public ushort        TextureCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderHeader6
    {
        public ShaderHeader5 Super;
        public uint          Unk;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Resource
    {
        public uint               Crc32;
        public uint               NameOffset;
        public ushort             NameSize;
        public ShaderResourceType Type;
        public ushort             Slot;
        public ushort             Size;
    }
}
