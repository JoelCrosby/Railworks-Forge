namespace RailworksForge.Core.Packaging;

// Exposes the zip that follows an .rwp header as a standalone seekable stream, so ZipArchive can read it from disk.
public sealed class OffsetReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _offset;

    public OffsetReadStream(Stream inner, long offset)
    {
        _inner = inner;
        _offset = offset;
        _inner.Position = offset;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _inner.Length - _offset;

    public override long Position
    {
        get => _inner.Position - _offset;
        set => _inner.Position = value + _offset;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _inner.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        return _inner.Read(buffer);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _inner.ReadAsync(buffer, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        Position = position;

        return position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
