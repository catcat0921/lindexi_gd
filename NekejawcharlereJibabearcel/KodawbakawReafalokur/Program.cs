﻿// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Diagnostics;
using System.Net;

var httpClientHandler = new HttpClientHandler();
httpClientHandler.MaxRequestContentBufferSize = 1024 * 1024;
var httpClient = new HttpClient(httpClientHandler);
httpClient.Timeout = TimeSpan.FromMinutes(30); // 即使在传输过程中，有网络传输，但是超过了时间，依然炸掉

var streamContent = new StreamContent(new FakeStream(1024_0000));
var cancellationTokenSource = new CancellationTokenSource();
var uploadHttpContent = new UploadHttpContent(streamContent, cancellationTokenSource);

var result = await httpClient.PostAsync("http://127.0.0.1:12367/Upload", uploadHttpContent, cancellationTokenSource.Token);
Console.WriteLine(await result.Content.ReadAsStringAsync());

try
{
    streamContent = new StreamContent(new FakeStream(1024_0000));
    cancellationTokenSource = new CancellationTokenSource();
    uploadHttpContent = new UploadHttpContent(streamContent, cancellationTokenSource);

    await httpClient.PostAsync("http://127.0.0.1:12367/UploadTimeout", uploadHttpContent, cancellationTokenSource.Token);
    uploadHttpContent.SetIsFinished();
}
catch (TimeoutException e)
{
    Console.WriteLine(e);
}

Console.WriteLine("Hello, World!");

class FakeStream : Stream
{
    public FakeStream(long length)
    {
        Length = length;
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Length > Position)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[i + offset] = (byte) i;
            }

            Position += count;
            return count;
        }

        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get; set; }
}

class UploadHttpContent : HttpContent
{
    public UploadHttpContent(HttpContent content, CancellationTokenSource tokenSource, TimeSpan? timeout = null)
    {
        _content = content;
        _tokenSource = tokenSource;
        _stream = content.ReadAsStream();
        _timeout = timeout ?? TimeSpan.FromSeconds(10);
    }

    private TimeSpan _timeout;

    private readonly HttpContent _content;
    private Stream _stream;
    private CancellationTokenSource _tokenSource;

    private bool _isFinished;
    private readonly Stopwatch _stopwatch = new Stopwatch();

    public void SetIsFinished() => _isFinished = true;

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        int count;

        StartDog();

        while ((count = _stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            // 这里存在一个问题是如果先读取完成了缓存，然后发送慢了，依然会炸掉
            _stopwatch.Restart();

            await stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, count), _tokenSource.Token);
            Console.WriteLine(DateTime.Now);
        }
    }

    private async void StartDog()
    {
        while (!_isFinished)
        {
            await Task.Delay(_timeout / 2);
            if (_isFinished)
            {
                return;
            }

            if (_stopwatch.Elapsed > _timeout)
            {
                _tokenSource.Cancel();
                return;
            }
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _content.ReadAsStream().Length;
        return true;
    }
}