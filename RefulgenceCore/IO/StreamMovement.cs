namespace Refulgence.IO;

public ref struct StreamMovement(Stream stream, bool restoreOnDisposal = true)
{
    private readonly long _savedPosition    = stream.Position;
    public           bool RestoreOnDisposal = restoreOnDisposal;

    public void Dispose()
    {
        if (RestoreOnDisposal) {
            stream.Position = _savedPosition;
        }
    }
}
