using System.Net.Security;
using System.Runtime.InteropServices;

namespace RuijieAC.MCP.Utils;

public class StatusLineFixStream(SslStream baseStream) : Stream
{
    private readonly Queue<byte> _buffer = new();
    private readonly List<byte> _lineBuffer = new(2048);
    private readonly byte[] _statusLinePrefix = "HTTP/1.1  "u8.ToArray();
    private bool _lineSliding;  // 当前读取的这一行是否直接滑入_buffer，当且仅当这一行不以_statusLinePrefix开头时为true

    public override bool CanRead => baseStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => baseStream.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    
    public override void Flush()
    {
        baseStream.Flush();
    }

    public override int Read(Span<byte> buffer)
    {
        if (_buffer.Count == 0)
            Readline();
        var read = _buffer.DequeueRangeTo(buffer);
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    /// <summary>
    /// Read at least one line from the stream, or until the stream is closed.
    /// </summary>
    private void Readline()
    {
        var lineBufferSpan = EnsureLineBufferSpan();
        int read;
        while ((read = baseStream.Read(lineBufferSpan[_lineBuffer.Count..])) > 0)
        {
            CollectionsMarshal.SetCount(_lineBuffer, _lineBuffer.Count + read);
            lineBufferSpan = lineBufferSpan[.._lineBuffer.Count];

            int left = 0, idx;
            if (_lineSliding)  // 如果这一行要滑入buffer，那就找一下读了的东西里面有没有换行符，没有的话就全部滑入，有的话就滑入到换行符
            {
                idx = lineBufferSpan.IndexOf((byte)'\n');
                if (idx == -1) idx = lineBufferSpan.Length - 1;
                else _lineSliding = false;
                left = idx + 1;
                _buffer.EnqueueRange(lineBufferSpan[..left]);
            }
            
            while (left < lineBufferSpan.Length && (idx = lineBufferSpan[left..].IndexOf((byte)'\n')) != -1)
            {
                FixLineAndAppend(lineBufferSpan.Slice(left, idx + 1));
                left += idx + 1;
            }
            
            _lineSliding = _lineSliding || (left < lineBufferSpan.Length && !IncompleteStartsWith(lineBufferSpan[left..], _statusLinePrefix));
            if (_lineSliding)  // 对的我们还要处理一次
            {
                _buffer.EnqueueRange(lineBufferSpan[left..]);
                left = lineBufferSpan.Length;
            }
            
            _lineBuffer.RemoveRange(0, left);
            if (left != 0) break;

            lineBufferSpan = EnsureLineBufferSpan();  // Reallocate if needed
        }
    }

    private void FixLineAndAppend(Span<byte> bytes)
    {
        if (bytes.StartsWith(_statusLinePrefix))
        {
            // 任何邪恶，终将绳之以法！
            _buffer.EnqueueRange(bytes[..(_statusLinePrefix.Length - 1)]);
            _buffer.EnqueueRange(bytes[_statusLinePrefix.Length..]);
            return;
        }
        
        _buffer.EnqueueRange(bytes);
    }

    private static bool IncompleteStartsWith(ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
    {
        return span.StartsWith(span.Length >= value.Length ? value : value[..span.Length]);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        baseStream.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        baseStream.Write(buffer);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        return baseStream.WriteAsync(buffer, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return baseStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        baseStream.WriteByte(value);
    }

    private Span<byte> EnsureLineBufferSpan()
    {
        if (_lineBuffer.Count == _lineBuffer.Capacity)
        {
            _lineBuffer.Add(0);  // Add a dummy byte to resize
            CollectionsMarshal.SetCount(_lineBuffer, _lineBuffer.Count - 1);  // Remove the dummy byte
        }
        
        var span = CollectionsMarshal.AsSpan(_lineBuffer);
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), _lineBuffer.Capacity);
    }

    protected override void Dispose(bool disposing)
    {
        baseStream.Dispose();
    }
}