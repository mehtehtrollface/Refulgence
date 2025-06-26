using Refulgence.Collections;
using Refulgence.IO;
using Refulgence.Sm5;

namespace Refulgence.Dxbc.Signature;

public sealed class SignatureDxPart : DxPart
{
    public uint                                          Unk1;
    public IndexedList<(string, uint), SignatureElement> Elements = new(element => (element.Name, element.SemanticIndex));

    public static SignatureDxPart FromBytes(ReadOnlySpan<byte> data)
    {
        var part = new SignatureDxPart();
        var reader = new SpanBinaryReader(data);
        var elementCount = reader.Read<uint>();
        part.Unk1 = reader.Read<uint>();
        for (var i = 0; i < elementCount; ++i) {
            part.Elements.Add(SignatureElement.Read(ref reader));
        }

        return part;
    }

    public static SignatureDxPart FromBytes(byte[] data)
        => FromBytes((ReadOnlySpan<byte>)data);

    public override void Dump(TextWriter writer)
    {
        writer.WriteLine("Signature");
        writer.WriteLine("    Name                 Index   Mask Register SysValue  Format   Used");
        writer.WriteLine("    -------------------- ----- ------ -------- -------- ------- ------");
        foreach (var element in Elements) {
            writer.WriteLine(
                $"    {element.Name,-20} {element.SemanticIndex,5}   {element.Mask.ToSpacedString()} {element.RegisterNum,8}{element.SystemValueType.ToTableString(),9} {element.ComponentType.ToTableString(),7}   {element.ReadWriteMask.ToSpacedString()}"
            );
        }
    }

    public override void WriteTo(Stream destination)
    {
        using var strings = new StringPool();
        var data = strings.Data;
        var orchestrator = new SubStreamOrchestrator();
        orchestrator.AddSubStreams(data);
        data.Write((uint)Elements.Count);
        data.Write(Unk1);
        data.Reserve(Elements.Count * SignatureElement.SizeInStream);
        foreach (var element in Elements) {
            element.WriteTo(strings, orchestrator);
        }

        data.Seek(0L, SeekOrigin.End);
        data.PadToAlignment(4, 0xAB);

        orchestrator.WriteAllTo(destination);
    }
}
