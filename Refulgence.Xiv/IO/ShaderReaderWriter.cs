using System.Runtime.InteropServices;
using Refulgence.IO;
using Refulgence.Text;
using Refulgence.Xiv.ShaderPackages;

namespace Refulgence.Xiv.IO;

internal static partial class ShaderReaderWriter
{
    public static ShaderPackage ReadPackage(ReadOnlySpan<byte> bytes)
    {
        var reader = new Reader(bytes, new("ShPk"u8));

        var result = new ShaderPackage(reader.CommonHeader.GraphicsPlatform);

        switch (reader.CommonHeader.Version & 0xFFFFFF00u) {
            case 0x00000B00u:
                new Package11Reader().Read(result, ref reader);
                break;
            case 0x00000D00u:
                new Package13Reader().Read(result, ref reader);
                break;
            default:
                throw new InvalidDataException($"Unrecognized ShPk version: 0x{reader.CommonHeader.Version:X8}");
        }

        reader.ExpectHeaderFullyConsumed();

        return result;
    }

    public static Shader ReadCode(ReadOnlySpan<byte> bytes)
    {
        var reader = new Reader(bytes, new("ShCd"u8));

        var programType = (ProgramType)(reader.CommonHeader.Version >> 24);
        if (!Enum.IsDefined(programType)) {
            throw new InvalidDataException($"Unrecognized ShCd program type: 0x{(uint)programType:X2}");
        }

        var result = new Shader(reader.CommonHeader.GraphicsPlatform, programType);

        switch (reader.CommonHeader.Version & 0x00FFFF00u) {
            case 0x00000300u:
                ReadCode3(result, ref reader);
                break;
            case 0x00000500u:
                ReadCode5(result, ref reader);
                break;
            case 0x00000600u:
                ReadCode6(result, ref reader, 0u);
                break;
            default:
                throw new InvalidDataException($"Unrecognized ShCd version: 0x{reader.CommonHeader.Version:X8}");
        }

        reader.ExpectHeaderFullyConsumed();

        return result;
    }

    public static void Write(ShaderPackage shaderPackage, Stream destination)
    {
        using var writer = new PackageWriter(shaderPackage, destination);
        writer.Write();
    }

    public static void Write(Shader shader, Stream destination)
    {
        using var writer = new CodeWriter(shader, destination);
        writer.Write();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CommonHeader
    {
        public InlineByteString<uint> Magic;
        public uint                   Version;
        public GraphicsPlatform       GraphicsPlatform;
        public uint                   FileSize;
        public uint                   BlobsOffset;
        public uint                   StringsOffset;
    }

    private ref struct Reader
    {
        public readonly CommonHeader CommonHeader;

        public readonly ReadOnlySpan<byte> Blobs;
        public readonly SpanBinaryReader   Strings;

        public SpanBinaryReader Header;

        public Reader(ReadOnlySpan<byte> bytes, InlineByteString<uint> magic)
        {
            var reader = new SpanBinaryReader(bytes);

            CommonHeader = reader.Read<CommonHeader>();
            if (magic != CommonHeader.Magic) {
                throw new InvalidDataException($"Invalid {magic} magic number: 0x{CommonHeader.Magic.Value:X8}");
            }

            if (!Enum.IsDefined(CommonHeader.GraphicsPlatform)) {
                throw new InvalidDataException($"Unrecognized {magic} graphics platform: 0x{(uint)CommonHeader.GraphicsPlatform:X8}");
            }

            if (CommonHeader.FileSize > reader.Length) {
                throw new InvalidDataException(
                    $"Invalid {magic} file size: buffer length 0x{bytes.Length:X}, but header says 0x{CommonHeader.FileSize:X}"
                );
            }

            if (CommonHeader.FileSize < reader.Length) {
                var position = reader.Position;
                reader = reader.SliceFrom(0, (int)CommonHeader.FileSize);
                reader.Position = position;
            }

            Blobs = reader.AsSpan()[(int)CommonHeader.BlobsOffset..(int)CommonHeader.StringsOffset];
            Strings = reader.SliceFrom((int)CommonHeader.StringsOffset, reader.Length - (int)CommonHeader.StringsOffset);

            Header = reader.SliceFrom(0, (int)CommonHeader.BlobsOffset);
            Header.Position = reader.Position;
        }

        public readonly void ExpectHeaderFullyConsumed()
        {
            if (Header.Remaining > 0) {
                throw new InvalidDataException($"{CommonHeader.Magic} file has unexpected extra header data");
            }
        }
    }

    private abstract class Writer : IDisposable
    {
        private static readonly nint FileSizeOffset      = Marshal.OffsetOf<CommonHeader>(nameof(CommonHeader.FileSize));
        private static readonly nint BlobsOffsetOffset   = Marshal.OffsetOf<CommonHeader>(nameof(CommonHeader.BlobsOffset));
        private static readonly nint StringsOffsetOffset = Marshal.OffsetOf<CommonHeader>(nameof(CommonHeader.StringsOffset));

        protected readonly MemoryStream Blobs;

        protected readonly Stream     Destination;
        protected readonly StringPool Strings;

        protected Writer(Stream destination)
        {
            if (!destination.CanWrite || !destination.CanSeek) {
                throw new NotSupportedException("Cannot write shader or shader package to non-writable or non-seekable stream.");
            }

            Blobs = new();
            Strings = new();

            Destination = destination;
        }

        protected abstract InlineByteString<uint> Magic { get; }

        protected abstract uint Version { get; }

        protected abstract GraphicsPlatform GraphicsPlatform { get; }

        public void Dispose()
        {
            Strings.Dispose();
            Blobs.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Write()
        {
            var headerPosition = Destination.Position;
            Destination.Write(
                new CommonHeader
                {
                    Magic = Magic,
                    Version = Version,
                    GraphicsPlatform = GraphicsPlatform,
                    FileSize = 0xFFFFFFFFu,
                    BlobsOffset = 0xFFFFFFFFu,
                    StringsOffset = 0xFFFFFFFFu,
                }
            );

            DoWrite();

            var blobsPosition = Destination.Position;
            Blobs.WriteTo(Destination);

            var stringsPosition = Destination.Position;
            Strings.WriteTo(Destination);

            var endPosition = Destination.Position;

            Destination.Position = headerPosition + FileSizeOffset;
            Destination.Write((uint)(endPosition - headerPosition));

            Destination.Position = headerPosition + BlobsOffsetOffset;
            Destination.Write((uint)(blobsPosition - headerPosition));

            Destination.Position = headerPosition + StringsOffsetOffset;
            Destination.Write((uint)(stringsPosition - headerPosition));

            Destination.Position = endPosition;
        }

        protected abstract void DoWrite();

        protected void WriteShaders(IEnumerable<Shader> shaders, uint unk)
        {
            foreach (var shader in shaders) {
                WriteShader(shader, unk);
            }
        }

        protected void WriteShader(Shader shader, uint unk)
        {
            var blobOffset = (uint)Blobs.Position;
            Blobs.Write(shader.AdditionalHeader, 0, shader.AdditionalHeader.Length);
            Blobs.Write(shader.ShaderBlob,       0, shader.ShaderBlob.Length);
            var blobSize = (uint)Blobs.Position - blobOffset;

            Destination.Write(
                new ShaderHeader6
                {
                    Super = new ShaderHeader5
                    {
                        Super = new ShaderHeader3
                        {
                            BlobOffset = blobOffset,
                            BlobSize = blobSize,
                            ConstantCount = (ushort)shader.ConstantBuffers.Count,
                            SamplerCount = (ushort)shader.Samplers.Count,
                        },
                        UavCount = (ushort)shader.Uavs.Count,
                        TextureCount = (ushort)shader.Textures.Count,
                    },
                    Unk = unk,
                }
            );

            WriteResources(shader.ConstantBuffers);
            WriteResources(shader.Samplers);
            WriteResources(shader.Uavs);
            WriteResources(shader.Textures);
        }

        protected void WriteResources(IEnumerable<ShaderResource> resources)
        {
            foreach (var resource in resources) {
                if (resource.Name.Value is null) {
                    throw new InvalidDataException();
                }

                var (nameOffset, nameSize) = Strings.FindOrAddString(resource.Name.Value);
                Destination.Write(
                    new Resource
                    {
                        Crc32 = resource.Name.Crc32,
                        NameOffset = (uint)nameOffset,
                        NameSize = (ushort)nameSize,
                        Type = resource.Type,
                        Slot = resource.Slot,
                        Size = resource.Size,
                    }
                );
            }
        }
    }
}
