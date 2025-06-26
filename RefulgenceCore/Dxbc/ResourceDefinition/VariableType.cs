using System.CodeDom.Compiler;
using Refulgence.Collections;
using Refulgence.IO;

namespace Refulgence.Dxbc.ResourceDefinition;

public sealed class VariableType
{
    public ShaderVariableClass                 Class;
    public ShaderVariableType                  Type;
    public ushort                              NumRows;
    public ushort                              NumColumns;
    public ushort                              NumElements;
    public IndexedList<string, VariableMember> Members = new(member => member.Name);
    public VariableType?                       BaseType;
    public VariableType?                       SuperType;
    public VariableType[]                      ImplementedInterfaces = [];
    public string                              Name                  = string.Empty;

    internal const int SizeInStream     = 16;
    internal const int Rd11SizeInStream = 20;

    internal static VariableType Read(ref SpanBinaryReader reader, ResourceDefinitionDxPart.VariableReadContext context)
    {
        var typeOffset = reader.Position;
        if (context.KnownTypes.TryGetValue(typeOffset, out var existingType)) {
            return existingType;
        }

        var type = new VariableType();
        context.KnownTypes.Add(typeOffset, type);
        try {
            type.Class = reader.Read<ShaderVariableClass>();
            type.Type = reader.Read<ShaderVariableType>();
            type.NumRows = reader.Read<ushort>();
            type.NumColumns = reader.Read<ushort>();
            type.NumElements = reader.Read<ushort>();
            var memberCount = reader.Read<ushort>();
            var memberOffset = reader.Read<uint>();
            if (memberCount > 0) {
                var memberReader = reader;
                memberReader.Position = (int)memberOffset;
                for (var i = 0; i < memberCount; ++i) {
                    type.Members.Add(VariableMember.Read(ref memberReader, context));
                }
            }

            if (context.Rd11) {
                var baseTypeOffset = reader.Read<uint>();
                if (baseTypeOffset > 0) {
                    var baseReader = reader;
                    baseReader.Position = (int)baseTypeOffset;
                    type.BaseType = Read(ref baseReader, context);
                }

                var superTypeOffset = reader.Read<uint>();
                if (superTypeOffset > 0) {
                    var superReader = reader;
                    superReader.Position = (int)superTypeOffset;
                    type.SuperType = Read(ref superReader, context);
                }

                var interfaceCount = reader.Read<uint>();
                var interfaceOffset = reader.Read<uint>();
                if (interfaceCount > 0 && interfaceOffset > 0) {
                    var interfaceReader = reader;
                    interfaceReader.Position = (int)interfaceOffset;
                    var interfaceOffsets = interfaceReader.Read<uint>((int)interfaceCount);
                    var interfaces = new VariableType[interfaceCount];
                    for (var i = 0; i < interfaceCount; ++i) {
                        interfaceReader.Position = (int)interfaceOffsets[i];
                        interfaces[i] = Read(ref interfaceReader, context);
                    }

                    type.ImplementedInterfaces = interfaces;
                }

                var nameOffset = reader.Read<uint>();
                type.Name = reader.ReadString((int)nameOffset);
            }
        } catch {
            context.KnownTypes.Remove(typeOffset);
            throw;
        }

        return type;
    }

    internal int Dump(IndentedTextWriter writer, uint offset)
    {
        if (Class is ShaderVariableClass.InterfacePointer) {
            writer.Write($"interface {Name}");
            return 10 + Name.Length;
        }

        if (Class is ShaderVariableClass.Struct) {
            writer.Write("struct");
            if (!string.IsNullOrEmpty(Name)) {
                writer.Write($" {Name}");
            }

            writer.WriteLine(" {");
            ++writer.Indent;
            foreach (var member in Members) {
                member.Dump(writer, offset);
            }

            --writer.Indent;
            writer.Write("}");

            return 1;
        }

        if (!string.IsNullOrEmpty(Name)) {
            writer.Write(Name);
            return Name.Length;
        }

        writer.Write($"<unknown {Class}>");
        return 10 + Class.ToString().Length;
    }

    internal long WriteTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context)
    {
        if (context.KnownTypes.TryGetValue(this, out var position)) {
            return position;
        }

        var stream = strings.Data;
        var orchestrator = context.Orchestrator;
        var interfacePosition = 0L;
        if (context.Rd11) {
            strings.FindOrAddString(Name);
        }

        if (context.Rd11) {
            BaseType?.WriteTo(strings, context);
            SuperType?.WriteTo(strings, context);
            if (ImplementedInterfaces.Length > 0) {
                interfacePosition = WriteListTo(strings, context, ImplementedInterfaces);
            }
        }

        foreach (var member in Members) {
            member.PreWriteTo(strings, context);
        }

        var membersWritten = Members.Count > 0 || Class == ShaderVariableClass.InterfaceClass;
        var memberPosition = membersWritten
            ? VariableMember.WriteListTo(strings, context, Members)
            : 0L;

        stream.PadToAlignment(4, 0xAB);
        stream.Reserve(SizeInStream + (context.Rd11 ? Rd11SizeInStream : 0));
        var typePosition = stream.Position;
        context.KnownTypes.Add(this, typePosition);
        stream.Write(Class);
        stream.Write(Type);
        stream.Write(NumRows);
        stream.Write(NumColumns);
        stream.Write(NumElements);
        stream.Write((ushort)Members.Count);
        if (membersWritten) {
            orchestrator.WriteDelayedPointer<uint>(stream, stream, memberPosition);
        } else {
            stream.Write(0u);
        }

        if (context.Rd11) {
            if (BaseType is not null) {
                orchestrator.WriteDelayedPointer<uint>(stream, stream, context.KnownTypes[BaseType]);
            } else {
                stream.Write(0u);
            }

            if (SuperType is not null) {
                orchestrator.WriteDelayedPointer<uint>(stream, stream, context.KnownTypes[SuperType]);
            } else {
                stream.Write(0u);
            }

            stream.Write(ImplementedInterfaces.Length);
            orchestrator.WriteDelayedPointer<uint>(stream, stream, interfacePosition);
            orchestrator.WriteDelayedPointer<uint>(stream, stream, Math.Max(strings.FindString(Name).Offset, 0));
        }

        return typePosition;
    }

    internal static long WriteListTo(StringPool strings, ResourceDefinitionDxPart.VariableWriteContext context,
        IEnumerable<VariableType> list)
    {
        var listSnapshot = list.ToArray();
        foreach (var (existingList, offset) in context.KnownTypeLists) {
            if (existingList.SequenceEqual(listSnapshot)) {
                return offset;
            }
        }

        foreach (var type in listSnapshot) {
            type.WriteTo(strings, context);
        }

        var stream = strings.Data;
        var orchestrator = context.Orchestrator;
        stream.PadToAlignment(4, 0xAB);
        var position = stream.Position;
        foreach (var type in listSnapshot) {
            orchestrator.WriteDelayedPointer<uint>(stream, stream, context.KnownTypes[type]);
        }

        context.KnownTypeLists.Add(new(listSnapshot, position));

        return position;
    }
}
