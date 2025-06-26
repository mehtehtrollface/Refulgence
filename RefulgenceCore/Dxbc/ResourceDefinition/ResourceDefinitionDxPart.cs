using System.CodeDom.Compiler;
using Refulgence.Collections;
using Refulgence.Interop;
using Refulgence.IO;
using Refulgence.Text;

namespace Refulgence.Dxbc.ResourceDefinition;

public sealed class ResourceDefinitionDxPart : DxPart
{
    public readonly IndexedList<string, ConstantBuffer>  ConstantBuffers = new(cbuffer => cbuffer.Name);
    public readonly IndexedList<string, ResourceBinding> Bindings        = new(binding => binding.Name);
    public          byte                                 MajorVersion;
    public          byte                                 MinorVersion;
    public          ProgramType                          ProgramType;
    public          D3DCompileFlags                      CompileFlags;
    public          string                               Creator = string.Empty;
    public          bool                                 IsRd11;
    public          uint                                 Rd11A;
    public          uint                                 Rd11B;
    public          uint                                 Rd11C;
    public          uint                                 Rd11D;
    public          uint                                 Rd11E;
    public          uint                                 Rd11F;
    public          uint                                 InterfaceSlotCount;

    internal const int HeaderSizeInStream         = 28;
    internal const int Rd11SizeInStream           = 32;
    internal const int BufferDefinitionLineLength = 44;

    public static ResourceDefinitionDxPart FromBytes(ReadOnlySpan<byte> data)
    {
        var rdef = new ResourceDefinitionDxPart();
        var reader = new SpanBinaryReader(data);
        var cbufferCount = reader.Read<uint>();
        var cbufferOffset = reader.Read<uint>();
        var bindCount = reader.Read<uint>();
        var bindOffset = reader.Read<uint>();
        rdef.MinorVersion = reader.Read<byte>();
        rdef.MajorVersion = reader.Read<byte>();
        rdef.ProgramType = reader.Read<ProgramType>();
        rdef.CompileFlags = reader.Read<D3DCompileFlags>();
        var creatorOffset = reader.Read<uint>();
        var extMagic = reader.Remaining >= 4 ? reader.Read<InlineByteString<uint>>() : default;
        if (extMagic == "RD11"u8) {
            rdef.IsRd11 = true;
            rdef.Rd11A = reader.Read<uint>();
            rdef.Rd11B = reader.Read<uint>();
            rdef.Rd11C = reader.Read<uint>();
            rdef.Rd11D = reader.Read<uint>();
            rdef.Rd11E = reader.Read<uint>();
            rdef.Rd11F = reader.Read<uint>();
            rdef.InterfaceSlotCount = reader.Read<uint>();
        }

        rdef.Creator = reader.ReadString((int)creatorOffset);

        var context = new VariableReadContext(rdef.IsRd11);
        reader.Position = (int)cbufferOffset;
        for (var i = 0; i < cbufferCount; ++i) {
            rdef.ConstantBuffers.Add(ConstantBuffer.Read(ref reader, context));
        }

        reader.Position = (int)bindOffset;
        for (var i = 0; i < bindCount; ++i) {
            rdef.Bindings.Add(ResourceBinding.Read(ref reader));
        }

        return rdef;
    }

    public static ResourceDefinitionDxPart FromBytes(byte[] data)
        => FromBytes((ReadOnlySpan<byte>)data);

    public override void Dump(TextWriter writer)
    {
        writer.WriteLine(IsRd11 ? "Resource Definitions (with RD11)" : "Resource Definitions");
        writer.WriteLine($"    Creator:     {Creator}");
        writer.WriteLine($"    Switches:    /T {ProgramType.ToAbbreviation()}_{MajorVersion}_{MinorVersion} {string.Join(" ", CompileFlags.ToCompilerSwitches().Select(sw => "/" + sw))}");
        if (IsRd11) {
            writer.WriteLine($"    RD11 fields: {Rd11A}, {Rd11B}, {Rd11C}, {Rd11D}, {Rd11E}, {Rd11F}");
            if (InterfaceSlotCount > 0) {
                writer.WriteLine($"    Interface slots: {InterfaceSlotCount}");
            }
        }

        if (ConstantBuffers.Count > 0) {
            writer.WriteLine();
            writer.WriteLine("    Buffer Definitions:");
            var indentedWriter = new IndentedTextWriter(writer);
            ++indentedWriter.Indent;
            foreach (var cbuffer in ConstantBuffers) {
                cbuffer.Dump(indentedWriter);
            }
        }

        if (Bindings.Count > 0) {
            writer.WriteLine();
            writer.WriteLine("    Resource Bindings:");
            writer.WriteLine("    Name                                 Type  Format         Dim      HLSL Bind  Count");
            writer.WriteLine("    ------------------------------ ---------- ------- ----------- -------------- ------");
            foreach (var bind in Bindings) {
                writer.WriteLine(
                    $"    {bind.Name,-30} {bind.InputType.ToTableString(),10} {$"{bind.ReturnType.ToTableString(bind.InputFlags)}",7} {bind.ViewDimension.ToTableString(bind.InputType, bind.NumSamples),11} {bind.InputType.ToBindPointString(bind.BindPoint),14} {bind.BindCount,6}"
                );
            }
        }
    }

    public override void WriteTo(Stream destination)
    {
        using var strings = new StringPool();
        var stream = strings.Data;

        var orchestrator = new SubStreamOrchestrator();
        orchestrator.AddSubStreams(stream);

        stream.Reserve(HeaderSizeInStream + (IsRd11 ? Rd11SizeInStream : 0));
        stream.Write((uint)ConstantBuffers.Count);
        var cbufferOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        stream.Write((uint)Bindings.Count);
        var bindOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        stream.Write(MinorVersion);
        stream.Write(MajorVersion);
        stream.Write(ProgramType);
        stream.Write(CompileFlags);
        var creatorOffset = orchestrator.WriteDelayedPointer<uint>(stream, stream, 0L);
        if (IsRd11) {
            stream.Write(new InlineByteString<uint>("RD11"u8));
            stream.Write(Rd11A);
            stream.Write(Rd11B);
            stream.Write(Rd11C);
            stream.Write(Rd11D);
            stream.Write(Rd11E);
            stream.Write(Rd11F);
            stream.Write(InterfaceSlotCount);
        }

        bindOffset.PointeePosition = stream.Position;
        stream.Reserve(Bindings.Count * ResourceBinding.SizeInStream);
        foreach (var bind in Bindings) {
            bind.WriteTo(strings, orchestrator);
        }

        if (ConstantBuffers.Count > 0) {
            stream.Seek(0L, SeekOrigin.End);
            stream.PadToAlignment(4, 0xAB);
            var context = new VariableWriteContext(orchestrator, IsRd11);
            cbufferOffset.PointeePosition = stream.Position;
            stream.Reserve(ConstantBuffers.Count * ConstantBuffer.SizeInStream);
            foreach (var cbuffer in ConstantBuffers) {
                cbuffer.WriteTo(strings, context);
            }
        }

        creatorOffset.PointeePosition = strings.FindOrAddString(Creator).Offset;
        stream.Seek(0L, SeekOrigin.End);
        stream.PadToAlignment(4, 0xAB);

        orchestrator.WriteAllTo(destination);
    }

    internal readonly struct VariableReadContext(bool rd11)
    {
        public readonly bool                          Rd11       = rd11;
        public readonly Dictionary<int, VariableType> KnownTypes = [];
    }

    internal readonly struct VariableWriteContext(SubStreamOrchestrator orchestrator, bool rd11)
    {
        public readonly SubStreamOrchestrator                      Orchestrator     = orchestrator;
        public readonly bool                                       Rd11             = rd11;
        public readonly Dictionary<VariableType, long>             KnownTypes       = [];
        public readonly List<KeyValuePair<VariableType[], long>>   KnownTypeLists   = [];
        public readonly List<KeyValuePair<VariableMember[], long>> KnownMemberLists = [];
    }
}
