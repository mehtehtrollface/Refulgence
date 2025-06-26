using System.Collections;
using System.Text.RegularExpressions;

namespace Refulgence.Text;

// See also https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar
public abstract class Lexer<T>(string input) : IEnumerator<T> where T : struct, IToken
{
    public readonly string   Input = input;
    private         Queue<T> _next = new();
    private         Position _nextPosition;

    public T Current { get; private set; }

    object IEnumerator.Current
        => Current;

    public void Reset()
    {
        _next.Clear();
        _nextPosition = default;
        Current = default;
    }

    public bool MoveNext()
    {
        Current = _next.Count > 0 ? _next.Dequeue() : ReadNext();
        return !Current.IsEnd;
    }

    public bool MoveNext(int count)
    {
        while (count-- > 0) {
            Current = _next.Count > 0 ? _next.Dequeue() : ReadNext();
        }

        return !Current.IsEnd;
    }

    public T Peek(int offset)
    {
        if (offset < 0) {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (offset == 0) {
            return Current;
        }

        if (offset <= _next.Count) {
            return _next.ElementAt(offset - 1);
        }

        T next;
        do {
            _next.Enqueue(next = ReadNext());
        } while (_next.Count < offset);

        return next;
    }

    private T ReadNext()
    {
        var next = ReadAt(_nextPosition);
        _nextPosition = next.Position + next.ValueSpan.EndPosition();
        return next;
    }

    protected abstract T ReadAt(Position position);

    protected void SkipWhiteSpace(ref Position position)
    {
        char ch;
        while (position.Offset < Input.Length && char.IsWhiteSpace(ch = Input[position.Offset])) {
            position.Increment(ch);
        }
    }

    protected bool MatchRegex(Regex regex, int offset, out Match match)
    {
        match = regex.Match(Input, offset);
        return match.Success;
    }

    void IDisposable.Dispose()
    {
    }
}
