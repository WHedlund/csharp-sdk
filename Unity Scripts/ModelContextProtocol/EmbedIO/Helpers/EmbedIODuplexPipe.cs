using System;
using System.IO;
using System.IO.Pipelines;

public sealed class EmbedIODuplexPipe : IDuplexPipe
{
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;

    public EmbedIODuplexPipe(Stream requestStream, Stream responseStream)
    {
        if (requestStream == null) throw new ArgumentNullException(nameof(requestStream));
        if (responseStream == null) throw new ArgumentNullException(nameof(responseStream));

        _reader = PipeReader.Create(requestStream);
        _writer = PipeWriter.Create(new AutoFlushingStream(responseStream));
    }

    public PipeReader Input => _reader;
    public PipeWriter Output => _writer;
}
