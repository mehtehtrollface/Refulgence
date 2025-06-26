using System.CodeDom.Compiler;
using Refulgence.IO;

namespace Refulgence.Dxbc.ResourceDefinition;

public readonly struct VariableMember(string name, VariableType type, uint start) : IEquatable<VariableMember>
{
    public readonly string       Name  = name;
    public readonly VariableType Type  = type;
    public readonly uint         Start = start;

    internal const int SizeInStream = 12;

    public override bool Equals(object? obj)
        => obj is VariableMember other && Equals(other);

    public bool Equals(VariableMember other)
        => Name == other.Name && Type.Equals(other.Type) && Start == other.Start;

    public override int GetHashCode()
        => HashCode.Combine(Name, Type, Start);

    internal static VariableMember Read(ref SpanBinaryReader reader, ResourceDefinitionDxPart.VariableReadContext context)
    {
        var nameOffset = reader.Read<uint>();
        var name = reader.ReadString((int)nameOffset);
        var typeOffset = reader.Read<uint>();
        var start = reader.Read<uint>();

        var typeReader = reader;
        typeReader.Position = (int)typeOffset;
        var type = VariableType.Read(ref typeReader, context);

        return new(name, type, start);
    }

    internal void Dump(IndentedTextWriter writer, uint offset)
    {
        var typeLength = Type.Dump(writer, offset + Start);
        writer.Write($" {Name}");
        var lineLength = typeLength + 2 + Name.Length + writer.Indent * 4;
        if (Type.NumElements > 0) {
            var arraySuffix = $"[{Type.NumElements}]";
            lineLength += arraySuffix.Length;
            writer.Write(arraySuffix);
        }

        writer.Write(';');
        if (lineLength < ResourceDefinitionDxPart.BufferDefinitionLineLength) {
            writer.Write(new string(' ', ResourceDefinitionDxPart.BufferDefinitionLineLength - lineLength));
        }

        writer.Write($"// Offset:{(Start == uint.MaxValue ? "N/A" : offset + Start),5}");
        writer.WriteLine();
    }

    internal void PreWriteTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context)
    {
        strings.FindOrAddString(Name);
        Type?.WriteTo(strings, context);
    }

    internal void WriteTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context)
    {
        var stream = strings.Data;
        var orchestrator = context.Orchestrator;
        orchestrator.WriteDelayedPointer<uint>(stream, stream, Math.Max(strings.FindString(Name).Offset, 0));
        if (Type is not null) {
            orchestrator.WriteDelayedPointer<uint>(stream, stream, context.KnownTypes[Type]);
        } else {
            stream.Write(0u);
        }

        stream.Write(Start);
    }

    internal static long WriteListTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context,
        IEnumerable<VariableMember> list)
    {
        var listSnapshot = list.ToArray();
        foreach (var (existingList, offset) in context.KnownMemberLists) {
            if (existingList.SequenceEqual(listSnapshot)) {
                return offset;
            }
        }

        var stream = strings.Data;
        stream.PadToAlignment(4, 0xAB);
        stream.Reserve(listSnapshot.Length * SizeInStream);

        var position = stream.Position;
        foreach (var member in listSnapshot) {
            member.WriteTo(strings, context);
        }

        context.KnownMemberLists.Add(new(listSnapshot, position));

        return position;
    }
}
