using System.Globalization;
using System.Text;

namespace Reggex.Desktop.IO;

public sealed record SourceTextLoadResult
{
    public bool IsSuccess { get; }
    public string? Content { get; }
    public string StatusMessage { get; }

    private SourceTextLoadResult(bool isSuccess, string? content, string statusMessage)
    {
        IsSuccess = isSuccess;
        Content = content;
        StatusMessage = statusMessage;
    }

    public static SourceTextLoadResult Success(string content, string statusMessage)
        => new(true, content, statusMessage);

    public static SourceTextLoadResult Failure(string errorMessage)
        => new(false, null, errorMessage);
}

public static class SourceTextLoader
{
    /// <summary>Maximum allowed file size in bytes (10 MB).</summary>
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public static async Task<SourceTextLoadResult> LoadFromStreamAsync(
        Stream stream,
        string fileName,
        long? knownSize = null,
        long maxSizeBytes = DefaultMaxFileSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(fileName);

        if (maxSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSizeBytes), "Max file size must be positive.");
        }

        var estimatedSize = knownSize ?? (stream.CanSeek ? stream.Length : (long?)null);
        if (estimatedSize.HasValue && estimatedSize.Value > maxSizeBytes)
        {
            return SourceTextLoadResult.Failure(
                $"File '{fileName}' exceeds the size limit ({FormatFileSize(estimatedSize.Value)}). Maximum supported size is {FormatFileSize(maxSizeBytes)}.");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
        var builder = new StringBuilder();
        var buffer = new char[8192];
        long totalCharsRead = 0;
        var maxChars = maxSizeBytes;

        int charsRead;
        while ((charsRead = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            totalCharsRead += charsRead;
            if (totalCharsRead > maxChars)
            {
                return SourceTextLoadResult.Failure(
                    $"File '{fileName}' exceeds the size limit. Maximum supported size is {FormatFileSize(maxSizeBytes)}.");
            }

            builder.Append(buffer, 0, charsRead);
        }

        return SourceTextLoadResult.Success(builder.ToString(), $"Loaded: {fileName}");
    }

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(bytes / 1024.0):F1} KB");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{(bytes / (1024.0 * 1024.0)):F1} MB");
    }
}
