namespace Refulgence.Text;

public record struct Position(int Offset, int Line, int Column)
{
    public void Increment(char ch)
    {
        ++Offset;
        if (ch == '\n') {
            ++Line;
            Column = 0;
        } else {
            ++Column;
        }
    }

    public override string ToString()
        => $"{Offset} ({Line + 1}:{Column + 1})";

    public static Position operator +(Position left, Position right)
        => new(left.Offset + right.Offset, left.Line + right.Line, right.Line > 0 ? right.Column : left.Column + right.Column);
}
