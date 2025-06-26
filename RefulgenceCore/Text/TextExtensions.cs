using System.Globalization;
using System.Text;

namespace Refulgence.Text;

public static class TextExtensions
{
    public static Position EndPosition(this ReadOnlySpan<char> value)
    {
        var position = new Position();
        foreach (var ch in value) {
            position.Increment(ch);
        }

        return position;
    }

    public static (string Before, string? After) SplitAfter(this string str, int index, int separatorLength)
        => index >= 0
            ? (str[..index], str[(index + separatorLength)..])
            : (str, null);

    public static (string? Before, string After) SplitBefore(this string str, int index, int separatorLength)
        => index >= 0
            ? (str[..index], str[(index + separatorLength)..])
            : (null, str);

    public static (string Before, string? After) SplitOnce(this string str, char separator)
        => str.SplitAfter(str.IndexOf(separator), 1);

    public static (string? Before, string After) SplitOnceLast(this string str, char separator)
        => str.SplitBefore(str.LastIndexOf(separator), 1);

    public static string PascalToCamelInvariant(this string str)
        => string.IsNullOrEmpty(str) ? str : $"{char.ToLowerInvariant(str[0])}{str[1..]}";

    public static string PascalToLowerWordsInvariant(this string str, char wordSeparator)
    {
        var sb = new StringBuilder(str.Length * 4 / 3);
        var first = true;
        foreach (var ch in str) {
            if (char.IsUpper(ch) && !first) {
                sb.Append(wordSeparator);
            }

            first = false;
            sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    public static void AppendImmediateToString(this StringBuilder sb, uint value, bool? floatingPoint)
    {
        var fValue = BitConverter.UInt32BitsToSingle(value);
        if (value == 0) {
            sb.Append('0');
        } else if (floatingPoint ?? float.IsNormal(fValue)) {
            var sfValue = fValue.ToString(CultureInfo.InvariantCulture);
            sb.Append(sfValue);
            if (!sfValue.Contains('.') && !sfValue.Contains('e') && !sfValue.Contains('E')) {
                sb.Append(".0");
            }
        } else if (value is < 10 or >= uint.MaxValue - 8) {
            sb.Append(CultureInfo.InvariantCulture, $"{unchecked((int)value)}");
        } else {
            sb.Append(CultureInfo.InvariantCulture, $"0x{value:X8}");
        }
    }

    public static void AppendImmediateToString(this StringBuilder sb, ulong value, bool? floatingPoint)
    {
        var dValue = BitConverter.UInt64BitsToDouble(value);
        if (value == 0) {
            sb.Append('0');
        } else if (floatingPoint ?? double.IsNormal(dValue)) {
            var sdValue = dValue.ToString(CultureInfo.InvariantCulture);
            sb.Append(sdValue);
            if (!sdValue.Contains('.') && !sdValue.Contains('e') && !sdValue.Contains('E')) {
                sb.Append(".0");
            }
        } else if (value is < 10 or >= ulong.MaxValue - 8) {
            sb.Append(CultureInfo.InvariantCulture, $"{unchecked((long)value)}");
        } else {
            sb.Append(CultureInfo.InvariantCulture, $"0x{value:X16}");
        }
    }
}
