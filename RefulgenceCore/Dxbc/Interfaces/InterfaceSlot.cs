using Refulgence.IO;

namespace Refulgence.Dxbc.Interfaces;

public sealed class InterfaceSlot
{
    public uint     SlotSpan;
    public ushort[] TypeIDs  = [];
    public uint[]   TableIDs = [];

    internal const int SizeInStream = 16;

    internal static InterfaceSlot Read(ref SpanBinaryReader reader)
    {
        var slot = new InterfaceSlot();
        slot.SlotSpan = reader.Read<uint>();
        var count = reader.Read<uint>();
        var typeIDsOffset = reader.Read<uint>();
        var tableIDsOffset = reader.Read<uint>();

        var idsReader = reader;
        idsReader.Position = (int)typeIDsOffset;
        slot.TypeIDs = idsReader.Read<ushort>((int)count).ToArray();
        idsReader.Position = (int)tableIDsOffset;
        slot.TableIDs = idsReader.Read<uint>((int)count).ToArray();

        return slot;
    }

    internal void WriteTo(Stream data, SubStreamOrchestrator orchestrator)
    {
        var count = Math.Min(TypeIDs.Length, TableIDs.Length);
        data.Write(SlotSpan);
        data.Write((uint)count);
        var typeIDsOffset = orchestrator.WriteDelayedPointer<uint>(data,  data, 0L);
        var tableIDsOffset = orchestrator.WriteDelayedPointer<uint>(data, data, 0L);

        using var _ = new StreamMovement(data);
        data.Seek(0L, SeekOrigin.End);

        data.PadToAlignment(4, 0xAB);
        typeIDsOffset.PointeePosition = data.Position;
        data.Write((ReadOnlySpan<ushort>)TypeIDs.AsSpan(0, count));

        data.PadToAlignment(4, 0xAB);
        tableIDsOffset.PointeePosition = data.Position;
        data.Write((ReadOnlySpan<uint>)TableIDs.AsSpan(0, count));
    }
}
