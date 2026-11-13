using System.Text.RegularExpressions;
using Reggex.Core;

namespace Reggex.Core.Tests;

public class DotNetRegexEngineSplitTests
{
    private readonly DotNetRegexEngine _engine = new();

    [Fact]
    public void SplitPreservesCapturedGroupsInTheOutput()
    {
        var result = _engine.Split(new RegexRequest(@"([,;])", "one,two;three"));

        Assert.Equal(RegexExecutionStatus.Success, result.Status);
        Assert.Collection(result.Segments,
            segment => Assert.Equal("one", segment),
            segment => Assert.Equal(",", segment),
            segment => Assert.Equal("two", segment),
            segment => Assert.Equal(";", segment),
            segment => Assert.Equal("three", segment));
        Assert.Null(result.Error);
    }

    [Fact]
    public void SplitPreservesEmptySegmentsAndReturnsInputWhenThereAreNoMatches()
    {
        var emptySegments = _engine.Split(new RegexRequest(",", ",a,,"));
        var unchangedInput = _engine.Split(new RegexRequest(",", "abc"));

        Assert.Collection(emptySegments.Segments,
            segment => Assert.Equal("", segment),
            segment => Assert.Equal("a", segment),
            segment => Assert.Equal("", segment),
            segment => Assert.Equal("", segment));
        Assert.Collection(unchangedInput.Segments, segment => Assert.Equal("abc", segment));
    }

    [Fact]
    public void SplitAppliesRequestOptions()
    {
        var result = _engine.Split(new RegexRequest(",", "a,b,C", RegexOptions.IgnoreCase));

        Assert.Collection(result.Segments,
            segment => Assert.Equal("a", segment),
            segment => Assert.Equal("b", segment),
            segment => Assert.Equal("C", segment));
    }

    [Fact]
    public void SplitMapsInvalidPatternWithDiagnostic()
    {
        var result = _engine.Split(new RegexRequest("(", "text"));

        Assert.Equal(RegexExecutionStatus.InvalidPattern, result.Status);
        Assert.Equal(RegexExecutionErrorCode.InvalidPattern, result.Error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Error.Details));
        Assert.True(result.Error.Position >= 0);
        Assert.Empty(result.Segments);
    }

    [Fact]
    public void SplitRespectsRequestTimeout()
    {
        var request = new RegexRequest(
            "(?:a|b)+z",
            new string('a', 20_000_000),
            executionTimeout: TimeSpan.FromMilliseconds(1));

        var result = _engine.Split(request);

        Assert.Equal(RegexExecutionStatus.TimedOut, result.Status);
        Assert.Equal(RegexExecutionErrorCode.Timeout, result.Error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Error.Details));
        Assert.Empty(result.Segments);
    }
}
