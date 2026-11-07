using System.Text.RegularExpressions;
using Reggex.Core;

namespace Reggex.Core.Tests;

public class RegexApiTests
{
    [Fact]
    public void RequestUsesTheDefaultTimeoutAndRegexOptions()
    {
        var request = new RegexRequest("a+", "aaa");

        Assert.Equal(TimeSpan.FromMilliseconds(500), request.ExecutionTimeout);
        Assert.Equal(RegexOptions.None, request.Options);
    }

    [Fact]
    public void RequestRejectsNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RegexRequest("a", "a", executionTimeout: TimeSpan.Zero));
    }

    [Fact]
    public void MatchAndGroupResultsPreserveMultipleCapturesAndUnsuccessfulGroups()
    {
        var captures = new[]
        {
            new RegexCaptureResult("a", 0, 1),
            new RegexCaptureResult("b", 1, 1)
        };
        var repeatedGroup = new RegexGroupResult("letters", true, "b", 1, 1, captures);
        var optionalGroup = new RegexGroupResult("optional", false, "", -1, 0, []);
        var match = new RegexMatchResult("ab", 0, 2, [repeatedGroup, optionalGroup]);

        Assert.Equal(2, match.Groups[0].Captures.Length);
        Assert.Equal("a", match.Groups[0].Captures[0].Value);
        Assert.Equal("b", match.Groups[0].Captures[1].Value);
        Assert.False(match.Groups[1].Success);
        Assert.Equal(-1, match.Groups[1].Index);
        Assert.Empty(match.Groups[1].Captures);
    }

    [Fact]
    public void ExecutionResultRepresentsSuccessAndFailuresWithConsistentData()
    {
        var match = new RegexMatchResult("a", 0, 1, []);
        var success = RegexExecutionResult.Succeeded([match]);
        var invalidPattern = RegexExecutionResult.InvalidPattern("Unclosed group", 3);
        var timeout = RegexExecutionResult.TimedOut();

        Assert.Equal(RegexExecutionStatus.Success, success.Status);
        Assert.Single(success.Matches);
        Assert.Null(success.Error);

        Assert.Equal(RegexExecutionStatus.InvalidPattern, invalidPattern.Status);
        Assert.Empty(invalidPattern.Matches);
        Assert.Equal(RegexExecutionErrorCode.InvalidPattern, invalidPattern.Error!.Code);
        Assert.Equal(3, invalidPattern.Error.Position);

        Assert.Equal(RegexExecutionStatus.TimedOut, timeout.Status);
        Assert.Empty(timeout.Matches);
        Assert.Equal(RegexExecutionErrorCode.Timeout, timeout.Error!.Code);
    }

    [Fact]
    public void CaptureResultRejectsInconsistentSpan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RegexCaptureResult("abc", 0, 2));
    }
}
