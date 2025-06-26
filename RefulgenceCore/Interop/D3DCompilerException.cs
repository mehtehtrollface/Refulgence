namespace Refulgence.Interop;

public sealed class D3DCompilerException(string message, Exception? innerException) : Exception(message, innerException)
{
}
