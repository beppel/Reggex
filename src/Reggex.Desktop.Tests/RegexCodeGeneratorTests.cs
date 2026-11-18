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
}
