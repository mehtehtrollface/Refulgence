namespace Refulgence.Sm5;

public ref struct OperandDecoder(ReadOnlySpan<uint> tokens)
{
    private readonly ReadOnlySpan<uint> _tokens = tokens;
    private          int                _offset;

    public int Offset
        => _offset;

    public Operand Current { get; private set; } = default;

    public OperandDecoder GetEnumerator()
        => this;

    public bool MoveNext()
    {
        if (_offset == _tokens.Length) {
            return false;
        }

        Current = Operand.Decode(_tokens, _offset, out _offset);

        return true;
    }
}
