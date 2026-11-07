using System.Text.RegularExpressions;
using Reggex.Core;

namespace Reggex.Core.Tests;

public class DotNetRegexEngineTests
{
    private readonly DotNetRegexEngine _engine = new();

    [Fact]
    public void ExecuteReturnsSimpleMatchWithSpan()
    {
        var result = Execute("cat", "a cat!");

        Assert.Equal(RegexExecutionStatus.Success, result.Status);
        var match = Assert.Single(result.Matches);
        Assert.Equal("cat", match.Value);
        Assert.Equal(2, match.Index);
        Assert.Equal(3, match.Length);
    }

    [Fact]
    public void ExecuteReturnsMultipleMatchesWithSpans()
    {
        var result = Execute("cat", "cat and cat");

        Assert.Collection(result.Matches,
            match => AssertMatch(match, "cat", 0, 3),
            match => AssertMatch(match, "cat", 8, 3));
    }

    [Fact]
    public void ExecuteReturnsSuccessWhenThereAreNoMatches()
    {
        var result = Execute("dog", "cat");

        Assert.Equal(RegexExecutionStatus.Success, result.Status);
        Assert.Empty(result.Matches);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ExecutePreservesNamedGroups()
    {
        var result = Execute(@"(?<word>\w+)-(?<digits>\d+)", "abc-123");
        var match = Assert.Single(result.Matches);

        AssertGroup(match.Groups[1], "word", true, "abc", 0, 3);
        AssertGroup(match.Groups[2], "digits", true, "123", 4, 3);
    }

    [Fact]
    public void ExecutePreservesMultipleCapturingGroups()
    {
        var result = Execute(@"(ab)(cd)", "abcd");
        var match = Assert.Single(result.Matches);

        AssertGroup(match.Groups[1], "1", true, "ab", 0, 2);
        AssertGroup(match.Groups[2], "2", true, "cd", 2, 2);
    }

    [Fact]
    public void ExecuteRepresentsUnmatchedOptionalGroups()
    {
        var result = Execute(@"(?<required>a)(?<optional>b)?", "a");
        var match = Assert.Single(result.Matches);
        var optionalGroup = match.Groups[2];

        Assert.Equal("optional", optionalGroup.Name);
        Assert.False(optionalGroup.Success);
        Assert.Equal(string.Empty, optionalGroup.Value);
        Assert.Equal(-1, optionalGroup.Index);
        Assert.Equal(0, optionalGroup.Length);
        Assert.Empty(optionalGroup.Captures);
    }

    [Fact]
    public void ExecuteDistinguishesSuccessfullyCapturedEmptyString()
    {
        var result = Execute(@"(?<empty>a*)", "");
        var match = Assert.Single(result.Matches);
        var group = match.Groups[1];

        Assert.True(group.Success);
        Assert.Equal(string.Empty, group.Value);
        Assert.Equal(0, group.Index);
        Assert.Equal(0, group.Length);
        var capture = Assert.Single(group.Captures);
        Assert.Equal(string.Empty, capture.Value);
        Assert.Equal(0, capture.Index);
        Assert.Equal(0, capture.Length);
    }

    [Fact]
    public void ExecutePreservesAllCapturesFromRepeatedGroup()
    {
        var result = Execute(@"(?<letter>[a-z])+", "abc");
        var match = Assert.Single(result.Matches);
        var group = match.Groups[1];

        Assert.Equal("letter", group.Name);
        Assert.Equal("c", group.Value);
        Assert.Equal(2, group.Index);
        Assert.Equal(1, group.Length);
        Assert.Collection(group.Captures,
            capture => AssertCapture(capture, "a", 0, 1),
            capture => AssertCapture(capture, "b", 1, 1),
            capture => AssertCapture(capture, "c", 2, 1));
    }

    [Fact]
    public void ExecuteAppliesIgnoreCaseOption()
    {
        var result = Execute("cat", "CAT", RegexOptions.IgnoreCase);

        var match = Assert.Single(result.Matches);
        AssertMatch(match, "CAT", 0, 3);
    }

    [Fact]
    public void ExecuteAppliesMultilineOption()
    {
        var result = Execute("^foo$", "x\nfoo\ny", RegexOptions.Multiline);

        var match = Assert.Single(result.Matches);
        AssertMatch(match, "foo", 2, 3);
    }

    [Fact]
    public void ExecuteMapsInvalidPatternAndPreservesDiagnostic()
    {
        var result = Execute("(", "text");

        Assert.Equal(RegexExecutionStatus.InvalidPattern, result.Status);
        Assert.Empty(result.Matches);
        Assert.Equal(RegexExecutionErrorCode.InvalidPattern, result.Error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Error.Details));
        Assert.True(result.Error.Position >= 0);
    }

    [Fact]
    public void ExecuteMapsTimeoutForAdversarialPatternOnRepeatedRuns()
    {
        var input = new string('a', 20_000_000);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = Execute("(?:a|b)+z", input, timeout: TimeSpan.FromMilliseconds(1));

            Assert.Equal(RegexExecutionStatus.TimedOut, result.Status);
            Assert.Empty(result.Matches);
            Assert.Equal(RegexExecutionErrorCode.Timeout, result.Error!.Code);
            Assert.False(string.IsNullOrWhiteSpace(result.Error.Details));
        }
    }

    [Fact]
    public void ExecuteReturnsZeroLengthMatchesWithIndexes()
    {
        var result = Execute("", "ab");

        Assert.Collection(result.Matches,
            match => AssertMatch(match, "", 0, 0),
            match => AssertMatch(match, "", 1, 0),
            match => AssertMatch(match, "", 2, 0));
    }

    [Fact]
    public void ExecutePreservesUnicodeValueAndUtf16Span()
    {
        var result = Execute("😀", "café 😀!");

        var match = Assert.Single(result.Matches);
        AssertMatch(match, "😀", 5, 2);
    }

    [Fact]
    public void ExecuteSupportsEmptyPattern()
    {
        var result = Execute("", "");

        var match = Assert.Single(result.Matches);
        AssertMatch(match, "", 0, 0);
    }

    [Fact]
    public void ExecuteSupportsEmptyInput()
    {
        var result = Execute("a+", "");

        Assert.Equal(RegexExecutionStatus.Success, result.Status);
        Assert.Empty(result.Matches);
    }

    private RegexExecutionResult Execute(
        string pattern,
        string input,
        RegexOptions options = RegexOptions.None,
        TimeSpan? timeout = null)
        => _engine.Execute(new RegexRequest(pattern, input, options, timeout));

    private static void AssertMatch(RegexMatchResult match, string value, int index, int length)
    {
        Assert.Equal(value, match.Value);
        Assert.Equal(index, match.Index);
        Assert.Equal(length, match.Length);
    }

    private static void AssertGroup(
        RegexGroupResult group,
        string name,
        bool success,
        string value,
        int index,
        int length)
    {
        Assert.Equal(name, group.Name);
        Assert.Equal(success, group.Success);
        Assert.Equal(value, group.Value);
        Assert.Equal(index, group.Index);
        Assert.Equal(length, group.Length);
    }

    private static void AssertCapture(RegexCaptureResult capture, string value, int index, int length)
    {
        Assert.Equal(value, capture.Value);
        Assert.Equal(index, capture.Index);
        Assert.Equal(length, capture.Length);
    }
}
