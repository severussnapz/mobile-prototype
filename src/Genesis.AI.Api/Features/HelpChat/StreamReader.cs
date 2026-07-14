using System.Text;

namespace Genesis.AI.Tests.Api;

public sealed class StreamReader : IDisposable
{
    private readonly System.IO.StreamReader _inner;

    public StreamReader(Stream stream, Encoding encoding)
    {
        _inner = new System.IO.StreamReader(
            stream,
            encoding,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);
    }

    public Task<string> ReadToEndAsync()
    {
        return _inner.ReadToEndAsync();
    }

    public void Dispose()
    {
        _inner.Dispose();
    }
}