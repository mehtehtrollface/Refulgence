using System.CodeDom.Compiler;
using Refulgence.IO;

namespace Refulgence.Dxbc.ResourceDefinition;

public sealed class Variable
{
    public string              Name = string.Empty;
    public uint                Start;
    public uint                Size;
    public ShaderVariableFlags Flags;
    public VariableType?       Type;
    public byte[]              DefaultValue = [];
    public uint                TextureStart;
    public uint                TextureSize;
    public uint                SamplerStart;
    public uint                SamplerSize;

    internal const int SizeInStream     = 24;
    internal const int Rd11SizeInStream = 16;

    internal static Variable Read(ref SpanBinaryReader reader, ResourceDefinitionDxPart.VariableReadContext context)
    {
        var variable = new Variable();
        var nameOffset = reader.Read<uint>();
        variable.Name = reader.ReadString((int)nameOffset);
        variable.Start = reader.Read<uint>();
        variable.Size = reader.Read<uint>();
        variable.Flags = reader.Read<ShaderVariableFlags>();
        var typeOffset = reader.Read<uint>();
        var defaultOffset = reader.Read<uint>();
        if (defaultOffset > 0 && variable.Size > 0) {
            var defaultReader = reader;
            defaultReader.Position = (int)defaultOffset;
            variable.DefaultValue = defaultReader.Read<byte>((int)variable.Size).ToArray();
        }

        if (context.Rd11) {
            variable.TextureStart = reader.Read<uint>();
            variable.TextureSize = reader.Read<uint>();
            variable.SamplerStart = reader.Read<uint>();
            variable.SamplerSize = reader.Read<uint>();
        }

        var typeReader = reader;
        typeReader.Position = (int)typeOffset;
        variable.Type = VariableType.Read(ref typeReader, context);

        return variable;
    }

    internal void Dump(IndentedTextWriter writer)
    {
        int typeLength;
        if (Type is not null) {
            typeLength = Type.Dump(writer, Start);
        } else {
            writer.Write("<unknown type>");
            typeLength = 14;
        }

        writer.Write($" {Name}");
        var lineLength = typeLength + 2 + Name.Length + writer.Indent * 4;
        if (Type is not null && Type.NumElements > 0) {
            var arraySuffix = $"[{Type.NumElements}]";
            lineLength += arraySuffix.Length;
            writer.Write(arraySuffix);
        }

        writer.Write(';');
        if (lineLength < ResourceDefinitionDxPart.BufferDefinitionLineLength) {
            writer.Write(new string(' ', ResourceDefinitionDxPart.BufferDefinitionLineLength - lineLength));
        }

        writer.Write($"// Offset:{(Start == uint.MaxValue ? "N/A" : Start),5} Size:{(Start == uint.MaxValue ? "N/A" : Size),5}");
        if (!Flags.HasFlag(ShaderVariableFlags.Used)) {
            writer.Write(" [unused]");
        }

        writer.WriteLine();
    }

    internal void WriteTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context)
    {
        var stream = strings.Data;
        var orchestrator = context.Orchestrator;
        var nameOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        stream.Write(Start);
        stream.Write(Size);
        stream.Write(Flags);
        SubStreamOrchestrator.DelayedPointer typeOffset, defaultOffset;
        if (Type is not null) {
            typeOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        } else {
            stream.Write(0u);
            typeOffset = default;
        }

        if (DefaultValue.Length > 0) {
            defaultOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        } else {
            stream.Write(0u);
            defaultOffset = default;
        }

        if (context.Rd11) {
            stream.Write(TextureStart);
            stream.Write(TextureSize);
            stream.Write(SamplerStart);
            stream.Write(SamplerSize);
        }

        using var _ = new StreamMovement(stream);
        stream.Seek(0L, SeekOrigin.End);
        nameOffset.PointeePosition = strings.FindOrAddString(Name).Offset;
        if (Type is not null) {
            typeOffset.PointeePosition = Type.WriteTo(strings, context);
        }

        if (DefaultValue.Length > 0) {
            stream.PadToAlignment(4, 0xAB);
            defaultOffset.PointeePosition = stream.Position;
            stream.Write(DefaultValue, 0, Math.Min((int)Size, DefaultValue.Length));
            if (Size > DefaultValue.Length) {
                stream.WriteRepeat((int)Size - DefaultValue.Length, 0xAB);
            }
        }
    }
}
