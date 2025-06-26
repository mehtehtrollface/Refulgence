namespace Refulgence.Interop;

public interface ISourceFile
{
    ReadOnlySpan<byte> Contents { get; }

    ISourceFile Include(IncludeType type, string filename);
}
