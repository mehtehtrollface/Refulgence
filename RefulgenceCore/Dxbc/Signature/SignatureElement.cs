using Refulgence.IO;
using Refulgence.Sm5;

namespace Refulgence.Dxbc.Signature;

public sealed class SignatureElement
{
    public string                Name = string.Empty;
    public uint                  SemanticIndex;
    public SystemValueType       SystemValueType;
    public RegisterComponentType ComponentType;
    public uint                  RegisterNum;
    public ComponentMask         Mask;
    public ComponentMask         ReadWriteMask;
    public byte                  Stream;

    internal const int SizeInStream = 24;

    internal static SignatureElement Read(ref SpanBinaryReader reader)
    {
        var element = new SignatureElement();
        var nameOffset = reader.Read<uint>();
        element.Name = reader.ReadString((int)nameOffset);
        element.SemanticIndex = reader.Read<uint>();
        element.SystemValueType = reader.Read<SystemValueType>();
        element.ComponentType = reader.Read<RegisterComponentType>();
        element.RegisterNum = reader.Read<uint>();
        element.Mask = reader.Read<ComponentMask>();
        element.ReadWriteMask = reader.Read<ComponentMask>();
        element.Stream = reader.Read<byte>();
        reader.Skip(1);

        return element;
    }

    internal void WriteTo(StringPool strings, SubStreamOrchestrator orchestrator)
    {
        var stream = strings.Data;
        orchestrator.WriteDelayedPointer<uint>(stream, strings.Data, 0L)
            .PointeePosition = strings.FindOrAddString(Name, true).Offset;
        stream.Write(SemanticIndex);
        stream.Write((uint)SystemValueType);
        stream.Write((uint)ComponentType);
        stream.Write(RegisterNum);
        stream.Write(Mask);
        stream.Write(ReadWriteMask);
        stream.Write(Stream);
        stream.WriteByte(0);
    }
}
