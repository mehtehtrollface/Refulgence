using Refulgence.IO;

namespace Refulgence.Dxbc.Interfaces;

public sealed class ClassType
{
    public string Name = string.Empty;
    public ushort ID;
    public ushort ConstantBufferStride;
    public ushort Texture;
    public ushort Sampler;

    internal const int SizeInStream = 12;

    internal static ClassType Read(ref SpanBinaryReader reader)
    {
        var type = new ClassType();
        var nameOffset = reader.Read<uint>();
        type.Name = reader.ReadString((int)nameOffset);
        type.ID = reader.Read<ushort>();
        type.ConstantBufferStride = reader.Read<ushort>();
        type.Texture = reader.Read<ushort>();
        type.Sampler = reader.Read<ushort>();

        return type;
    }

    internal void WriteTo(Stream data, StringPool strings, SubStreamOrchestrator orchestrator)
    {
        orchestrator.WriteDelayedPointer<uint>(data, strings.Data, strings.FindOrAddString(Name).Offset);
        strings.Data.PadToAlignment(4, 0xAB);
        data.Write(ID);
        data.Write(ConstantBufferStride);
        data.Write(Texture);
        data.Write(Sampler);
    }
}
