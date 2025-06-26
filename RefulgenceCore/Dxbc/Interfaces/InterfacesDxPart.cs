using Refulgence.Collections;
using Refulgence.IO;

namespace Refulgence.Dxbc.Interfaces;

public sealed class InterfacesDxPart : DxPart
{
    public readonly IndexedList<string, ClassInstance> ClassInstances = new(instance => instance.Name);
    public readonly IndexedList<string, ClassType>     ClassTypes     = new(type => type.Name);
    public readonly List<InterfaceSlot>                InterfaceSlots = [];
    public          uint                               InterfaceSlotCount;
    public          uint                               Unk1;
    public          uint                               Unk2;

    internal const int SizeInStream = 36;

    public static InterfacesDxPart FromBytes(ReadOnlySpan<byte> data)
    {
        var part = new InterfacesDxPart();
        var reader = new SpanBinaryReader(data);
        var classInstanceCount = reader.Read<uint>();
        var classTypeCount = reader.Read<uint>();
        var interfaceSlotRecordCount = reader.Read<uint>();
        part.InterfaceSlotCount = reader.Read<uint>();
        var classInstanceOffset = reader.Read<uint>();
        var classTypeOffset = reader.Read<uint>();
        var interfaceSlotOffset = reader.Read<uint>();
        part.Unk1 = reader.Read<uint>();
        part.Unk2 = reader.Read<uint>();
        reader.Position = (int)classInstanceOffset;
        for (var i = 0; i < classInstanceCount; ++i) {
            part.ClassInstances.Add(ClassInstance.Read(ref reader));
        }

        reader.Position = (int)classTypeOffset;
        for (var i = 0; i < classTypeCount; ++i) {
            part.ClassTypes.Add(ClassType.Read(ref reader));
        }

        reader.Position = (int)interfaceSlotOffset;
        for (var i = 0; i < interfaceSlotRecordCount; ++i) {
            part.InterfaceSlots.Add(InterfaceSlot.Read(ref reader));
        }

        return part;
    }

    public override void Dump(TextWriter writer)
    {
        writer.WriteLine("Interfaces");
        writer.WriteLine($"    Unknowns: {Unk1}, {Unk2:X8}");

        writer.WriteLine();
        writer.WriteLine("    Available Class Types:");
        writer.WriteLine("    Name                             ID CB Stride Texture Sampler");
        writer.WriteLine("    ------------------------------ ---- --------- ------- -------");
        var i = 0u;
        foreach (var type in ClassTypes) {
            writer.WriteLine($"    {type.Name,-30} {i++,4} {type.ConstantBufferStride,9} {type.Texture,7} {type.Sampler,7}");
        }

        writer.WriteLine();
        writer.WriteLine("    Available Class Instances:");
        writer.WriteLine("    Name                        Type CB CB Offset Texture Sampler");
        writer.WriteLine("    --------------------------- ---- -- --------- ------- -------");
        foreach (var instance in ClassInstances) {
            writer.WriteLine(
                $"    {instance.Name,-27} {instance.Type,4} {instance.ConstantBuffer,2} {instance.ConstantBufferOffset,9} {(instance.Texture == ushort.MaxValue ? "-" : instance.Texture),7} {(instance.Sampler == ushort.MaxValue ? "-" : instance.Sampler),7}"
            );
        }

        writer.WriteLine();
        writer.WriteLine($"    Interface slots, {InterfaceSlotCount} total:");
        writer.WriteLine("                Slots");
        writer.WriteLine("    +----------+---------+---------------------------------------");
        i = 0;
        foreach (var slot in InterfaceSlots) {
            writer.Write($"    | Type ID  |{i,4}{(slot.SlotSpan > 1 ? $"-{i + slot.SlotSpan - 1}" : string.Empty),-5}|");
            foreach (var type in slot.TypeIDs) {
                writer.Write($"{type,-5}");
            }

            writer.WriteLine();
            writer.Write("    | Table ID |         |");
            foreach (var table in slot.TableIDs) {
                writer.Write($"{table,-5}");
            }

            writer.WriteLine();
            writer.WriteLine("    +----------+---------+---------------------------------------");
            i += slot.SlotSpan;
        }
    }

    public override void WriteTo(Stream destination)
    {
        using var data = new MemoryStream();
        using var strings = new StringPool();

        var orchestrator = new SubStreamOrchestrator();
        orchestrator.AddSubStreams(data, strings.Data);

        data.Reserve(
            SizeInStream + InterfaceSlots.Count * InterfaceSlot.SizeInStream + ClassInstances.Count * ClassInstance.SizeInStream
          + ClassTypes.Count * ClassType.SizeInStream
        );
        data.Write((uint)ClassInstances.Count);
        data.Write((uint)ClassTypes.Count);
        data.Write((uint)InterfaceSlots.Count);
        data.Write(InterfaceSlotCount);
        var classInstanceOffset = orchestrator.WriteDelayedPointer<uint>(data, data, 0L);
        var classTypeOffset = orchestrator.WriteDelayedPointer<uint>(data,     data, 0L);
        var interfaceSlotOffset = orchestrator.WriteDelayedPointer<uint>(data, data, 0L);
        data.Write(Unk1);
        data.Write(Unk2);

        interfaceSlotOffset.PointeePosition = data.Position;
        foreach (var slot in InterfaceSlots) {
            slot.WriteTo(data, orchestrator);
        }

        foreach (var instance in ClassInstances) {
            instance.PreWriteTo(strings);
        }

        classTypeOffset.PointeePosition = data.Position;
        foreach (var type in ClassTypes) {
            type.WriteTo(data, strings, orchestrator);
        }

        classInstanceOffset.PointeePosition = data.Position;
        foreach (var instance in ClassInstances) {
            instance.WriteTo(data, strings, orchestrator);
        }

        orchestrator.WriteAllTo(destination);
    }
}
