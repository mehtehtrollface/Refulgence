namespace Refulgence.Dxbc;

public sealed class OpaqueDxPart(byte[] data) : DxPart
{
    public override void Dump(TextWriter writer)
        => writer.WriteLine($"{data.Length} (0x{data.Length:X}) bytes (opaque)");

    public override byte[] ToBytes()
        => data;

    public override void WriteTo(Stream destination)
        => destination.Write(data, 0, data.Length);
}
