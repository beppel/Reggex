using System.Text.RegularExpressions;

namespace Reggex.Core;

/// <summary>Executes requests with the .NET regular expression engine.</summary>
public sealed class DotNetRegexEngine : IRegexEngine
{
    /// <inheritdoc />
    public RegexExecutionResult Execute(RegexRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Regex regex;
        try
        {
            regex = new Regex(request.Pattern, request.Options, request.ExecutionTimeout);
        }
        catch (RegexParseException exception)
        {
            return RegexExecutionResult.InvalidPattern(exception.Message, exception.Offset);
        }
        catch (ArgumentException exception) when (string.Equals(exception.ParamName, "options", StringComparison.OrdinalIgnoreCase))
        {
            return RegexExecutionResult.InvalidOptions(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RegexExecutionResult.InvalidPattern(exception.Message);
        }

        try
        {
            var groupNames = regex.GetGroupNames();
            var rawMatches = regex.Matches(request.InputText)
                .Cast<Match>()
                .Take(request.MaximumResults == int.MaxValue ? int.MaxValue : request.MaximumResults + 1)
                .ToList();

            var isTruncated = rawMatches.Count > request.MaximumResults;
            var matchesToConvert = isTruncated ? rawMatches.Take(request.MaximumResults) : rawMatches;

            var matches = matchesToConvert
                .Select(match => new RegexMatchResult(
                    match.Value,
                    match.Index,
                    match.Length,
                    groupNames.Select(name => ConvertGroup(match.Groups[name], name))))
                .ToArray();

            return RegexExecutionResult.Succeeded(matches, isTruncated);
        }
        catch (RegexMatchTimeoutException exception)
        {
            return RegexExecutionResult.TimedOut(exception.Message);
        }
    }

    /// <summary>Applies a .NET replacement expression using the request's options and timeout.</summary>
    public RegexReplacementResult Replace(RegexRequest request, string replacement)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(replacement);

        try
        {
            var regex = new Regex(request.Pattern, request.Options, request.ExecutionTimeout);
            return RegexReplacementResult.Succeeded(regex.Replace(request.InputText, replacement));
        }
        catch (RegexParseException exception)
        {
            return RegexReplacementResult.InvalidPattern(exception.Message, exception.Offset);
        }
        catch (ArgumentException exception) when (string.Equals(exception.ParamName, "options", StringComparison.OrdinalIgnoreCase))
        {
            return RegexReplacementResult.InvalidOptions(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RegexReplacementResult.InvalidPattern(exception.Message);
        }
        catch (RegexMatchTimeoutException exception)
        {
            return RegexReplacementResult.TimedOut(exception.Message);
        }
    }

    /// <summary>Splits the input text using the request's options and timeout.</summary>
    public RegexSplitResult Split(RegexRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var regex = new Regex(request.Pattern, request.Options, request.ExecutionTimeout);
            return RegexSplitResult.Succeeded(regex.Split(request.InputText));
        }
        catch (RegexParseException exception)
        {
            return RegexSplitResult.InvalidPattern(exception.Message, exception.Offset);
        }
        catch (ArgumentException exception) when (string.Equals(exception.ParamName, "options", StringComparison.OrdinalIgnoreCase))
        {
            return RegexSplitResult.InvalidOptions(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return RegexSplitResult.InvalidPattern(exception.Message);
        }
        catch (RegexMatchTimeoutException exception)
        {
            return RegexSplitResult.TimedOut(exception.Message);
        }
    }

    private static RegexGroupResult ConvertGroup(Group group, string name)
    {
        if (!group.Success)
        {
            return new RegexGroupResult(name, false, string.Empty, -1, 0, []);
        }

        var captures = group.Captures
            .Cast<Capture>()
            .Select(capture => new RegexCaptureResult(capture.Value, capture.Index, capture.Length));

        return new RegexGroupResult(
            name,
            true,
            group.Value,
            group.Index,
            group.Length,
            captures);
    }
}
