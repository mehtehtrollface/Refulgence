namespace Refulgence.IO;

public interface IBytesConvertible
{
    byte[] ToBytes()
    {
        using var buffer = new MemoryStream();
        WriteTo(buffer);

        return buffer.ToArray();
    }

    void WriteTo(Stream destination);
}
