using Refulgence.IO;

namespace Refulgence.Dxbc.Interfaces;

public sealed class ClassInstance
{
    public string Name = string.Empty;
    public ushort Type;
    public ushort Unk;
    public ushort ConstantBuffer;
    public ushort ConstantBufferOffset;
    public ushort Texture;
    public ushort Sampler;

    internal const int SizeInStream = 16;

    internal static ClassInstance Read(ref SpanBinaryReader reader)
    {
        var instance = new ClassInstance();
        var nameOffset = reader.Read<uint>();
        instance.Name = reader.ReadString((int)nameOffset);
        instance.Type = reader.Read<ushort>();
        instance.Unk = reader.Read<ushort>();
        instance.ConstantBuffer = reader.Read<ushort>();
        instance.ConstantBufferOffset = reader.Read<ushort>();
        instance.Texture = reader.Read<ushort>();
        instance.Sampler = reader.Read<ushort>();

        return instance;
    }

    internal void PreWriteTo(StringPool strings)
    {
        strings.FindOrAddString(Name);
        strings.Data.PadToAlignment(4, 0xAB);
    }

    internal void WriteTo(Stream data, StringPool strings, SubStreamOrchestrator orchestrator)
    {
        orchestrator.WriteDelayedPointer<uint>(data, strings.Data, strings.FindString(Name).Offset);
        data.Write(Type);
        data.Write(Unk);
        data.Write(ConstantBuffer);
        data.Write(ConstantBufferOffset);
        data.Write(Texture);
        data.Write(Sampler);
    }
}
