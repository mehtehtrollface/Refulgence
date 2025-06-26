namespace Refulgence.Xiv.ShaderPackages;

public sealed class MaterialParameter(Name name, ushort byteOffset, ushort byteSize)
{
    public ushort ByteOffset = byteOffset;
    public ushort ByteSize   = byteSize;
    public Name   Name       = name;
}
