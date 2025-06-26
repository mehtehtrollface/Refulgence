namespace Refulgence.Dxbc.ResourceDefinition;

public enum ProgramType : ushort
{
    ComputeShader  = 0x4353,
    DomainShader   = 0x4453,
    GeometryShader = 0x4753,
    HullShader     = 0x4853,
    VertexShader   = 0xFFFE,
    PixelShader    = 0xFFFF,
}
