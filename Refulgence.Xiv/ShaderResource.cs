namespace Refulgence.Xiv;

public sealed class ShaderResource(Name name, ShaderResourceType type, ushort slot, ushort size)
{
    public Name               Name = name;
    public ShaderResourceType Type = type;
    public ushort             Size = size;
    public ushort             Slot = slot;
}
