namespace Refulgence.Dxbc.ResourceDefinition;

public enum ShaderVariableClass : ushort
{
    Scalar = 0,
    Vector,
    MatrixRows,
    MatrixColumns,
    Object,
    Struct,
    InterfaceClass,
    InterfacePointer,
}
