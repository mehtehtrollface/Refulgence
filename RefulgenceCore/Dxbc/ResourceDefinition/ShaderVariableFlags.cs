namespace Refulgence.Dxbc.ResourceDefinition;

public enum ShaderVariableFlags : uint
{
    UserPacked = 0x1,
    Used = 0x2,
    InterfacePointer = 0x4,
    InterfaceParameter = 0x8,
}
