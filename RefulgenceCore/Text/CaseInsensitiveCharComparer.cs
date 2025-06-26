namespace Refulgence.Text;

public sealed class CaseInsensitiveCharComparer : IEqualityComparer<char>, IComparer<char>
{
    public static readonly CaseInsensitiveCharComparer Instance = new();

    private CaseInsensitiveCharComparer()
    {
    }

    public bool Equals(char x, char y)
        => char.ToLowerInvariant(x) == char.ToLowerInvariant(y);

    public int GetHashCode(char obj)
        => char.ToLowerInvariant(obj).GetHashCode();

    public int Compare(char x, char y)
        => char.ToLowerInvariant(x).CompareTo(char.ToLowerInvariant(y));
}
