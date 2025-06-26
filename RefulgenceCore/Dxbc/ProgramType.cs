namespace Refulgence.Dxbc;

public enum ProgramType : ushort
{
    PixelShader    = 0x0,
    VertexShader   = 0x1,
    GeometryShader = 0x2,
    HullShader     = 0x3,
    DomainShader   = 0x4,
    ComputeShader  = 0x5,
}
