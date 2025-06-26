using System.CodeDom.Compiler;
using Refulgence.Collections;
using Refulgence.IO;

namespace Refulgence.Dxbc.ResourceDefinition;

public sealed class ConstantBuffer
{
    public          string                              Name = string.Empty;
    public          uint                                Size;
    public          ConstantBufferFlags                 Flags;
    public          ConstantBufferType                  Type;
    public readonly IndexedList<string, Variable> Variables = new(variable => variable.Name);

    internal const int SizeInStream = 24;

    internal static ConstantBuffer Read(ref SpanBinaryReader reader, ResourceDefinitionDxPart.VariableReadContext context)
    {
        var cbuffer = new ConstantBuffer();
        var nameOffset = reader.Read<uint>();
        cbuffer.Name = reader.ReadString((int)nameOffset);
        var variableCount = reader.Read<uint>();
        var variableOffset = reader.Read<uint>();
        cbuffer.Size = reader.Read<uint>();
        cbuffer.Flags = reader.Read<ConstantBufferFlags>();
        cbuffer.Type = reader.Read<ConstantBufferType>();

        var varReader = reader;
        varReader.Position = (int)variableOffset;
        for (var i = 0; i < variableCount; ++i) {
            cbuffer.Variables.Add(Variable.Read(ref varReader, context));
        }

        return cbuffer;
    }

    internal void Dump(IndentedTextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine($"{Type.ToDeclarationKeyword()} {Name}");
        writer.WriteLine("{");
        ++writer.Indent;
        foreach (var variable in Variables) {
            variable.Dump(writer);
        }

        --writer.Indent;
        writer.WriteLine("}");
    }

    internal void WriteTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context)
    {
        var stream = strings.Data;
        var orchestrator = context.Orchestrator;
        orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L)
                    .PointeePosition = strings.FindOrAddString(Name, true).Offset;
        stream.Write((uint)Variables.Count);
        var variableOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        stream.Write(Size);
        stream.Write(Flags);
        stream.Write(Type);

        using var _ = new StreamMovement(stream);
        stream.Seek(0L, SeekOrigin.End);
        stream.PadToAlignment(4, 0xAB);
        variableOffset.PointeePosition = stream.Position;
        stream.Reserve(Variables.Count * (Variable.SizeInStream + (context.Rd11 ? Variable.Rd11SizeInStream : 0)));
        foreach (var variable in Variables) {
            variable.WriteTo(strings, context);
        }
    }
}
