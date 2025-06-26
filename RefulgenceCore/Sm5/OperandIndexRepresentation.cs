namespace Refulgence.Sm5;

public enum OperandIndexRepresentation : byte
{
    Immediate32             = 0,
    Immediate64             = 1,
    Relative                = 2,
    Immediate32PlusRelative = 3,
    Immediate64PlusRelative = 4,
}
