using Reggex.Core;

namespace Reggex.Core.Tests;

public class DotNetRegexEngineReplaceTests
{
    private readonly DotNetRegexEngine _engine = new();

    [Fact]
    public void ReplaceSupportsNamedAndNumberedGroupReferences()
    {
        var request = new RegexRequest(@"(?<word>\w+)-(\d+)", "abc-123 xyz-9");

        var result = _engine.Replace(request, "${word}:$1");

        Assert.Equal(RegexExecutionStatus.Success, result.Status);
        Assert.Equal("abc:123 xyz:9", result.Output);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ReplaceReturnsOriginalInputWhenThereAreNoMatches()
    {
        var request = new RegexRequest("dog", "cat", executionTimeout: TimeSpan.FromSeconds(1));

        var result = _engine.Replace(request, "fox");

        Assert.Equal(RegexExecutionStatus.Success, result.Status);
        Assert.Equal("cat", result.Output);
    }

    [Fact]
    public void ReplaceMapsInvalidPatternWithDiagnostic()
    {
        var request = new RegexRequest("(", "text");

        var result = _engine.Replace(request, "replacement");

        Assert.Equal(RegexExecutionStatus.InvalidPattern, result.Status);
        Assert.Equal(RegexExecutionErrorCode.InvalidPattern, result.Error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Error.Details));
        Assert.True(result.Error.Position >= 0);
        Assert.Equal(string.Empty, result.Output);
    }

    [Fact]
    public void ReplaceRespectsRequestTimeout()
    {
        var request = new RegexRequest(
            "(?:a|b)+z",
            new string('a', 20_000_000),
            executionTimeout: TimeSpan.FromMilliseconds(1));

        var result = _engine.Replace(request, "replacement");

        Assert.Equal(RegexExecutionStatus.TimedOut, result.Status);
        Assert.Equal(RegexExecutionErrorCode.Timeout, result.Error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Error.Details));
        Assert.Equal(string.Empty, result.Output);
    }
}
