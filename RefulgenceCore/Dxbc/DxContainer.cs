using System.Diagnostics.CodeAnalysis;
using Refulgence.Dxbc.Interfaces;
using Refulgence.Dxbc.ResourceDefinition;
using Refulgence.Dxbc.Signature;
using Refulgence.IO;
using Refulgence.Text;

namespace Refulgence.Dxbc;

// https://llvm.org/docs/DirectX/DXContainer.html
// http://timjones.io/blog/archive/2015/09/02/parsing-direct3d-shader-bytecode
public sealed class DxContainer : IBytesConvertible
{
    public readonly OrderedDictionary<InlineByteString<uint>, DxPart> Parts = [];

    public bool TryGetResourceDefinition([MaybeNullWhen(false)] out ResourceDefinitionDxPart part)
        => TryGetPart(new("RDEF"u8), out part);

    public bool TryGetInputSignature([MaybeNullWhen(false)] out SignatureDxPart part)
        => TryGetPart(new("ISGN"u8), out part);

    public bool TryGetOutputSignature([MaybeNullWhen(false)] out SignatureDxPart part)
        => TryGetPart(new("OSGN"u8), out part);

    public bool TryGetPatchConstantSignature([MaybeNullWhen(false)] out SignatureDxPart part)
        => TryGetPart(new("PCSG"u8), out part);

    public bool TryGetInterfaces([MaybeNullWhen(false)] out InterfacesDxPart part)
        => TryGetPart(new("IFCE"u8), out part);

    public bool TryGetShader([MaybeNullWhen(false)] out ShaderDxPart part)
        => TryGetPart(new("SHEX"u8), out part) || TryGetPart(new("SHDR"u8), out part);

    public bool TryGetStats([MaybeNullWhen(false)] out StatsDxPart part)
        => TryGetPart(new("STAT"u8), out part);

    public bool TryGetPart<T>(InlineByteString<uint> type, [MaybeNullWhen(false)] out T part) where T : DxPart
    {
        if (!Parts.TryGetValue(type, out var rawPart)) {
            part = null;
            return false;
        }

        if (rawPart is OpaqueDxPart && !typeof(T).IsAssignableFrom(typeof(OpaqueDxPart))) {
            rawPart = DxPart.Create(type, rawPart.ToBytes());
        } else if (typeof(T) == typeof(OpaqueDxPart) && rawPart is not OpaqueDxPart) {
            rawPart = new OpaqueDxPart(rawPart.ToBytes());
        }

        part = rawPart as T;
        return part is not null;
    }

    public static DxContainer FromBytes(ReadOnlySpan<byte> bytes, bool verifyDigest = true, bool opaque = false)
    {
        var reader = new SpanBinaryReader(bytes);
        var magic = reader.Read<InlineByteString<uint>>();
        if (magic != "DXBC"u8) {
            throw new InvalidDataException($"Invalid DxContainer magic number: 0x{magic.Value:X8}");
        }

        var digest = reader.Read<byte>(16);

        var major = reader.Read<ushort>();
        var minor = reader.Read<ushort>();
        if (major != 1) {
            throw new InvalidDataException($"Unsupported DxContainer version: {major}.{minor}");
        }

        var size = reader.Read<uint>();
        if (bytes.Length < size) {
            throw new InvalidDataException(
                $"Invalid DxContainer file size: buffer length 0x{bytes.Length:X}, but header says 0x{size:X}"
            );
        }

        if (verifyDigest && !DxDigest.Verify(bytes[0x14..(int)size], digest)) {
            throw new InvalidDataException("Incorrect DxContainer digest");
        }

        var partCount = reader.Read<uint>();
        var partOffsets = reader.Read<uint>((int)partCount);

        var result = new DxContainer();
        for (var i = 0; i < partCount; ++i) {
            var partReader = new SpanBinaryReader(bytes[(int)partOffsets[i]..(int)size]);
            var type = partReader.Read<InlineByteString<uint>>();
            var length = partReader.Read<uint>();
            var partData = partReader.Read<byte>((int)length);
            result.Parts.Add(type, opaque ? new OpaqueDxPart(partData.ToArray()) : DxPart.Create(type, partData));
        }

        return result;
    }

    public void Dump(TextWriter writer)
    {
        writer.WriteLine("-- DxContainer --");
        writer.WriteLine();
        foreach (var (type, part) in Parts) {
            writer.Write($"{type}: ");
            part.Dump(writer);
            writer.WriteLine();
        }
    }

    public void WriteTo(Stream destination)
    {
        var headerPosition = destination.Position;
        destination.Write(new InlineByteString<uint>("DXBC"u8));
        var digestPosition = destination.Position;
        destination.Write(new byte[DxDigest.Length], 0, DxDigest.Length);
        destination.Write((ushort)1);
        destination.Write((ushort)0);
        var sizePosition = destination.Position;
        destination.Write(0u);
        destination.Write((uint)Parts.Count);
        var offsetsPosition = destination.Position;
        for (var i = 0; i < Parts.Count; ++i) {
            destination.Write(0u);
        }

        var partPositions = new long[Parts.Count];
        for (var i = 0; i < Parts.Count; ++i) {
            var (type, part) = Parts.GetAt(i);
            partPositions[i] = destination.Position;
            destination.Write(type);
            destination.Write(0u);
            part.WriteTo(destination);
        }

        var endPosition = destination.Position;

        destination.Position = sizePosition;
        destination.Write((uint)(endPosition - headerPosition));

        destination.Position = offsetsPosition;
        for (var i = 0; i < Parts.Count; ++i) {
            destination.Write((uint)(partPositions[i] - headerPosition));
        }

        for (var i = 1; i < Parts.Count; ++i) {
            destination.Position = partPositions[i - 1] + 4;
            destination.Write((uint)(partPositions[i] - partPositions[i - 1] - 8));
        }

        if (Parts.Count > 0) {
            destination.Position = partPositions[^1] + 4;
            destination.Write((uint)(endPosition - partPositions[^1] - 8));
        }

        Span<byte> digest = stackalloc byte[DxDigest.Length];
        destination.Position = digestPosition + DxDigest.Length;
        if (destination.TryGetSegment((int)(endPosition - destination.Position), out var segment)) {
            DxDigest.Calculate(segment, digest);
        } else {
            var data = new byte[endPosition - destination.Position];
            destination.ReadFully(data, 0, data.Length);
            DxDigest.Calculate(data, digest);
        }

        destination.Position = digestPosition;
        destination.Write(digest);

        destination.Position = endPosition;
    }
}
