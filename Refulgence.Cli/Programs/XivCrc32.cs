using Refulgence.Xiv;

namespace Refulgence.Cli.Programs;

public static class XivCrc32
{
    public static int Run(ReadOnlySpan<string> inputs)
    {
        Console.WriteLine("CRC32    Input");
        Console.WriteLine("-------- --------------------------------------------------");
        foreach (var input in inputs) {
            Console.WriteLine($"{new Name(input).Crc32:X8} {input}");
        }

        return 0;
    }
}
