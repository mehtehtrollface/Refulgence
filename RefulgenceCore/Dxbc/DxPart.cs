using Refulgence.Dxbc.Interfaces;
using Refulgence.Dxbc.ResourceDefinition;
using Refulgence.Dxbc.Signature;
using Refulgence.IO;
using Refulgence.Text;

namespace Refulgence.Dxbc;

public abstract class DxPart : IBytesConvertible
{
    public abstract void Dump(TextWriter writer);

    public virtual byte[] ToBytes()
    {
        using var buffer = new MemoryStream();
        WriteTo(buffer);

        return buffer.ToArray();
    }

    public abstract void WriteTo(Stream destination);

    public static DxPart Create(InlineByteString<uint> type, ReadOnlySpan<byte> data)
    {
        if (type == "RDEF"u8) {
            return ResourceDefinitionDxPart.FromBytes(data);
        } else if (type == "ISGN"u8 || type == "OSGN"u8 || type == "PCSG"u8) {
            return SignatureDxPart.FromBytes(data);
        } else if (type == "IFCE"u8) {
            return InterfacesDxPart.FromBytes(data);
        } else if (type == "SHDR"u8 || type == "SHEX"u8) {
            return ShaderDxPart.FromBytes(data);
        } else if (type == "STAT"u8) {
            return StatsDxPart.FromBytes(data);
        } else {
            return new OpaqueDxPart(data.ToArray());
        }
    }
}
