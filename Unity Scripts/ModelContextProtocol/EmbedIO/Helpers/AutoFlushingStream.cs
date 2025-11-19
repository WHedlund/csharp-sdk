using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class AutoFlushingStream : Stream
{
    private readonly Stream _innerStream;

    public AutoFlushingStream(Stream innerStream)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush() => _innerStream.Flush();

    public override async Task FlushAsync(CancellationToken cancellationToken) =>
        await _innerStream.FlushAsync(cancellationToken).ConfigureAwait(false);

    public override int Read(byte[] buffer, int offset, int count) =>
        _innerStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) =>
        _innerStream.Seek(offset, origin);

    public override void SetLength(long value) =>
        _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
        _innerStream.Flush(); // synchronous flush
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _innerStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        await _innerStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _innerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        await _innerStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _innerStream.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _innerStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
