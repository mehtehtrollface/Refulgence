namespace Refulgence.Sm5;

public ref struct InstructionDecoder(ReadOnlySpan<uint> tokens)
{
    private readonly ReadOnlySpan<uint> _tokens = tokens;
    private          int                _offset;

    public Instruction Current { get; private set; } = default;

    public InstructionDecoder GetEnumerator()
        => this;

    public bool MoveNext()
    {
        if (_offset == _tokens.Length) {
            return false;
        }

        Current = Instruction.Decode(_tokens, _offset, out _offset);

        return true;
    }
}
