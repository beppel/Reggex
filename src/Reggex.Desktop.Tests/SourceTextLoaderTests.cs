using System.Text;
using Reggex.Desktop.IO;

namespace Reggex.Desktop.Tests;

public class SourceTextLoaderTests
{
    [Fact]
    public async Task LoadFromStreamReturnsSuccessForValidContent()
    {
        const string text = "Line 1\nLine 2\nLine 3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var result = await SourceTextLoader.LoadFromStreamAsync(stream, "sample.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal(text, result.Content);
        Assert.Equal("Loaded: sample.txt", result.StatusMessage);
    }

    [Fact]
    public async Task LoadFromStreamReturnsFailureWhenKnownSizeExceedsLimit()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("small"));

        var result = await SourceTextLoader.LoadFromStreamAsync(
            stream,
            "large.txt",
            knownSize: 50 * 1024 * 1024,
            maxSizeBytes: 10 * 1024 * 1024);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Content);
        Assert.Contains("exceeds the size limit", result.StatusMessage);
        Assert.Contains("50.0 MB", result.StatusMessage);
        Assert.Contains("10.0 MB", result.StatusMessage);
    }

    [Fact]
    public async Task LoadFromStreamReturnsFailureWhenSeekableStreamLengthExceedsLimit()
    {
        var bytes = new byte[200];
        using var stream = new MemoryStream(bytes);

        var result = await SourceTextLoader.LoadFromStreamAsync(
            stream,
            "data.log",
            maxSizeBytes: 100);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Content);
        Assert.Contains("exceeds the size limit", result.StatusMessage);
        Assert.Contains("200 B", result.StatusMessage);
    }

    [Fact]
    public async Task LoadFromStreamReturnsFailureWhenNonSeekableStreamExceedsLimitDuringRead()
    {
        var text = new string('x', 300);
        using var underlying = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var nonSeekable = new NonSeekableStreamWrapper(underlying);

        var result = await SourceTextLoader.LoadFromStreamAsync(
            nonSeekable,
            "stream.txt",
            maxSizeBytes: 100);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Content);
        Assert.Contains("exceeds the size limit", result.StatusMessage);
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10 * 1024 * 1024, "10.0 MB")]
    public void FormatFileSizeFormatsExpectedUnits(long bytes, string expected)
    {
        var formatted = SourceTextLoader.FormatFileSize(bytes);
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public async Task LoadFromStreamRejectsInvalidArguments()
    {
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SourceTextLoader.LoadFromStreamAsync(null!, "test.txt"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SourceTextLoader.LoadFromStreamAsync(stream, null!));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            SourceTextLoader.LoadFromStreamAsync(stream, "test.txt", maxSizeBytes: 0));
    }

    private sealed class NonSeekableStreamWrapper(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
    }
}
