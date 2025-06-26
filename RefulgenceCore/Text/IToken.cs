namespace Refulgence.Text;

public interface IToken
{
    bool IsEnd { get; }

    Position Position { get; }

    ReadOnlySpan<char> ValueSpan { get; }
}
