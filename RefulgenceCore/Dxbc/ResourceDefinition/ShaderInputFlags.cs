namespace Refulgence.Dxbc.ResourceDefinition;

[Flags]
public enum ShaderInputFlags : uint
{
    UserPacked        = 0x1,
    ComparisonSampler = 0x2,
    TextureComponent0 = 0x4,
    TextureComponent1 = 0x8,
    Unused            = 0x10,
}
