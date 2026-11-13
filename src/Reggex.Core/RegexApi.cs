using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Reggex.Core;

/// <summary>Describes a regular expression execution request.</summary>
public sealed record RegexRequest
{
    /// <summary>The regular expression pattern to execute.</summary>
    public string Pattern { get; }

    /// <summary>The input text against which the pattern is evaluated.</summary>
    public string InputText { get; }

    /// <summary>Options applied by the selected regex engine.</summary>
    public RegexOptions Options { get; }

    /// <summary>The maximum duration allowed for execution.</summary>
    public TimeSpan ExecutionTimeout { get; }

    /// <summary>The default execution timeout used when none is provided.</summary>
    public static TimeSpan DefaultExecutionTimeout { get; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Creates a request, defaulting to no regex options and a 500 ms timeout.</summary>
    public RegexRequest(
        string pattern,
        string inputText,
        RegexOptions options = RegexOptions.None,
        TimeSpan? executionTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(inputText);

        var timeout = executionTimeout ?? DefaultExecutionTimeout;
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(executionTimeout), "The execution timeout must be positive.");
        }

        Pattern = pattern;
        InputText = inputText;
        Options = options;
        ExecutionTimeout = timeout;
    }
}

/// <summary>Executes regular expression requests without exposing engine-specific objects.</summary>
public interface IRegexEngine
{
    /// <summary>Executes a request and returns matches or structured failure information.</summary>
    RegexExecutionResult Execute(RegexRequest request);

    /// <summary>Applies a replacement expression and returns replaced text or structured failure information.</summary>
    RegexReplacementResult Replace(RegexRequest request, string replacement);

    /// <summary>Splits the input text and returns segments or structured failure information.</summary>
    RegexSplitResult Split(RegexRequest request);
}

/// <summary>Identifies the outcome of a regex execution.</summary>
public enum RegexExecutionStatus
{
    /// <summary>The request completed successfully.</summary>
    Success,

    /// <summary>The pattern could not be parsed by the selected engine.</summary>
    InvalidPattern,

    /// <summary>The execution exceeded the request timeout.</summary>
    TimedOut
}

/// <summary>Identifies a structured execution error.</summary>
public enum RegexExecutionErrorCode
{
    /// <summary>The pattern is invalid for the selected engine.</summary>
    InvalidPattern,

    /// <summary>The execution exceeded the configured timeout.</summary>
    Timeout
}

/// <summary>Provides machine-readable details about an execution failure.</summary>
public sealed record RegexExecutionError(
    RegexExecutionErrorCode Code,
    string? Details = null,
    int? Position = null);

/// <summary>Contains the matches or error produced by a regex execution.</summary>
public sealed record RegexExecutionResult
{
    /// <summary>The execution outcome.</summary>
    public RegexExecutionStatus Status { get; }

    /// <summary>All complete matches, empty when execution did not succeed.</summary>
    public ImmutableArray<RegexMatchResult> Matches { get; }

    /// <summary>Structured error details for a failed execution; otherwise null.</summary>
    public RegexExecutionError? Error { get; }

    private RegexExecutionResult(
        RegexExecutionStatus status,
        ImmutableArray<RegexMatchResult> matches,
        RegexExecutionError? error)
    {
        Status = status;
        Matches = matches;
        Error = error;
    }

    /// <summary>Creates a successful result from the matches returned by an engine.</summary>
    public static RegexExecutionResult Succeeded(IEnumerable<RegexMatchResult> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        return new RegexExecutionResult(RegexExecutionStatus.Success, matches.ToImmutableArray(), null);
    }

    /// <summary>Creates a result for a pattern rejected by the engine.</summary>
    public static RegexExecutionResult InvalidPattern(string? details = null, int? position = null)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The error position cannot be negative.");
        }

        return new RegexExecutionResult(
            RegexExecutionStatus.InvalidPattern,
            ImmutableArray<RegexMatchResult>.Empty,
            new RegexExecutionError(RegexExecutionErrorCode.InvalidPattern, details, position));
    }

    /// <summary>Creates a result for an execution that exceeded its timeout.</summary>
    public static RegexExecutionResult TimedOut(string? details = null)
        => new(
            RegexExecutionStatus.TimedOut,
            ImmutableArray<RegexMatchResult>.Empty,
            new RegexExecutionError(RegexExecutionErrorCode.Timeout, details));
}

/// <summary>Contains replaced text or structured failure information from a replacement operation.</summary>
public sealed record RegexReplacementResult
{
    /// <summary>The replacement operation outcome.</summary>
    public RegexExecutionStatus Status { get; }

    /// <summary>The resulting text on success; empty when replacement failed.</summary>
    public string Output { get; }

    /// <summary>Structured error details for a failed replacement; otherwise null.</summary>
    public RegexExecutionError? Error { get; }

    private RegexReplacementResult(RegexExecutionStatus status, string output, RegexExecutionError? error)
    {
        Status = status;
        Output = output;
        Error = error;
    }

    /// <summary>Creates a successful replacement result.</summary>
    public static RegexReplacementResult Succeeded(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        return new RegexReplacementResult(RegexExecutionStatus.Success, output, null);
    }

    /// <summary>Creates a result for a pattern rejected by the engine.</summary>
    public static RegexReplacementResult InvalidPattern(string? details = null, int? position = null)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The error position cannot be negative.");
        }

        return new RegexReplacementResult(
            RegexExecutionStatus.InvalidPattern,
            string.Empty,
            new RegexExecutionError(RegexExecutionErrorCode.InvalidPattern, details, position));
    }

    /// <summary>Creates a result for a replacement operation that exceeded its timeout.</summary>
    public static RegexReplacementResult TimedOut(string? details = null)
        => new(
            RegexExecutionStatus.TimedOut,
            string.Empty,
            new RegexExecutionError(RegexExecutionErrorCode.Timeout, details));
}

/// <summary>Contains the segments or error produced by a split operation.</summary>
public sealed record RegexSplitResult
{
    /// <summary>The split operation outcome.</summary>
    public RegexExecutionStatus Status { get; }

    /// <summary>The resulting segments on success; empty when splitting failed.</summary>
    public ImmutableArray<string> Segments { get; }

    /// <summary>Structured error details for a failed split; otherwise null.</summary>
    public RegexExecutionError? Error { get; }

    private RegexSplitResult(
        RegexExecutionStatus status,
        ImmutableArray<string> segments,
        RegexExecutionError? error)
    {
        Status = status;
        Segments = segments;
        Error = error;
    }

    /// <summary>Creates a successful split result.</summary>
    public static RegexSplitResult Succeeded(IEnumerable<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        return new RegexSplitResult(RegexExecutionStatus.Success, segments.ToImmutableArray(), null);
    }

    /// <summary>Creates a result for a pattern rejected by the engine.</summary>
    public static RegexSplitResult InvalidPattern(string? details = null, int? position = null)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The error position cannot be negative.");
        }

        return new RegexSplitResult(
            RegexExecutionStatus.InvalidPattern,
            ImmutableArray<string>.Empty,
            new RegexExecutionError(RegexExecutionErrorCode.InvalidPattern, details, position));
    }

    /// <summary>Creates a result for a split operation that exceeded its timeout.</summary>
    public static RegexSplitResult TimedOut(string? details = null)
        => new(
            RegexExecutionStatus.TimedOut,
            ImmutableArray<string>.Empty,
            new RegexExecutionError(RegexExecutionErrorCode.Timeout, details));
}

/// <summary>Represents one complete match in the input text.</summary>
public sealed record RegexMatchResult
{
    /// <summary>The text matched by the complete expression.</summary>
    public string Value { get; }

    /// <summary>The zero-based position of the match in the input.</summary>
    public int Index { get; }

    /// <summary>The number of UTF-16 code units in the match.</summary>
    public int Length { get; }

    /// <summary>Groups captured by this match, including the engine's full-match group when provided.</summary>
    public ImmutableArray<RegexGroupResult> Groups { get; }

    public RegexMatchResult(string value, int index, int length, IEnumerable<RegexGroupResult> groups)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(groups);
        ValidateSpan(value, index, length);

        Value = value;
        Index = index;
        Length = length;
        Groups = groups.ToImmutableArray();
    }

    internal static void ValidateSpan(string value, int index, int length)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "The index cannot be negative.");
        }

        if (length < 0 || length != value.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "The length must equal the value's UTF-16 length.");
        }
    }
}

/// <summary>Represents one named or numbered group in a match.</summary>
public sealed record RegexGroupResult
{
    /// <summary>The group's name, or its numeric name for unnamed groups.</summary>
    public string Name { get; }

    /// <summary>Whether the group captured a value.</summary>
    public bool Success { get; }

    /// <summary>The group's captured value; empty when an optional group did not participate.</summary>
    public string Value { get; }

    /// <summary>The group's zero-based input position, or -1 when it did not capture.</summary>
    public int Index { get; }

    /// <summary>The number of UTF-16 code units captured by the group.</summary>
    public int Length { get; }

    /// <summary>Individual captures made by this group, in capture order.</summary>
    public ImmutableArray<RegexCaptureResult> Captures { get; }

    public RegexGroupResult(
        string name,
        bool success,
        string value,
        int index,
        int length,
        IEnumerable<RegexCaptureResult> captures)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(captures);

        var immutableCaptures = captures.ToImmutableArray();
        if (success)
        {
            RegexMatchResult.ValidateSpan(value, index, length);
        }
        else if (value.Length != 0 || index != -1 || length != 0 || !immutableCaptures.IsEmpty)
        {
            throw new ArgumentException(
                "An unsuccessful group must have an empty value, index -1, zero length, and no captures.",
                nameof(success));
        }

        Name = name;
        Success = success;
        Value = value;
        Index = index;
        Length = length;
        Captures = immutableCaptures;
    }
}

/// <summary>Represents one individual capture, including a capture within a repeated group.</summary>
public sealed record RegexCaptureResult
{
    /// <summary>The captured text.</summary>
    public string Value { get; }

    /// <summary>The zero-based position of the capture in the input.</summary>
    public int Index { get; }

    /// <summary>The number of UTF-16 code units in the capture.</summary>
    public int Length { get; }

    public RegexCaptureResult(string value, int index, int length)
    {
        ArgumentNullException.ThrowIfNull(value);
        RegexMatchResult.ValidateSpan(value, index, length);

        Value = value;
        Index = index;
        Length = length;
    }
}
