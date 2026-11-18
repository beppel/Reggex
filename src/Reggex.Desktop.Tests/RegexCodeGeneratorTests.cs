using System.Text.RegularExpressions;
using Reggex.Desktop.CodeGeneration;

namespace Reggex.Desktop.Tests;

public class RegexCodeGeneratorTests
{
    [Fact]
    public void GenerateIncludesNonBacktrackingOption()
    {
        var options = new RegexCodeGenerationOptions(
            @"\d+",
            string.Empty,
            RegexOptions.NonBacktracking,
            RegexCodeSampleKind.Find,
            UseInstanceMethod: false,
            AssumeUsingDirectives: false,
            UseVerbatimStrings: true,
            GenerateLoop: false);

        var code = RegexCodeGenerator.Generate(options);

        Assert.Contains("System.Text.RegularExpressions.RegexOptions.NonBacktracking", code);
    }

    [Theory]
    [InlineData(RegexCodeSampleKind.Find, "var mc = MyRegex().Matches(\"text\");")]
    [InlineData(RegexCodeSampleKind.Replace, "var result = MyRegex().Replace(\"text\", \"\");")]
    [InlineData(RegexCodeSampleKind.Split, "var parts = MyRegex().Split(\"text\");")]
    public void GenerateSupportsGeneratedRegex(RegexCodeSampleKind sampleKind, string expectedOperation)
    {
        var options = new RegexCodeGenerationOptions(
            @"\d+",
            string.Empty,
            RegexOptions.IgnoreCase,
            sampleKind,
            UseInstanceMethod: false,
            AssumeUsingDirectives: true,
            UseVerbatimStrings: true,
            GenerateLoop: false,
            UseGeneratedRegex: true);

        var code = RegexCodeGenerator.Generate(options);

        Assert.Contains("partial class Program", code);
        Assert.Contains("[GeneratedRegex(@\"\\d+\", RegexOptions.IgnoreCase)]", code);
        Assert.Contains("private static partial Regex MyRegex();", code);
        Assert.Contains(expectedOperation, code);
    }
}
