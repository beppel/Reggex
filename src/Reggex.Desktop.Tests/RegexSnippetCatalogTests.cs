using System.Text.RegularExpressions;
using Reggex.Core;
using Reggex.Desktop.Snippets;

namespace Reggex.Desktop.Tests;

public class RegexSnippetCatalogTests
{
    [Fact]
    public void PatternSnippetsAreValidDotNetPatternsWhenPlaceholdersAreFilled()
    {
        foreach (var category in RegexSnippetCatalog.PatternCategories)
        {
            foreach (var snippet in category.Snippets)
            {
                var pattern = FillPlaceholders(snippet.Text, snippet.Label);
                if (category.Name == "Quantifiers")
                {
                    pattern = "a" + pattern;
                }

                if (snippet.Label.StartsWith("Backreference", StringComparison.Ordinal) ||
                    snippet.Label.StartsWith("Condition on named group", StringComparison.Ordinal))
                {
                    pattern = "(?<capture>a)" + pattern;
                }

                var exception = Record.Exception(() => new Regex(pattern));
                Assert.True(exception is null, $"{snippet.Label}: {exception?.Message}");
            }
        }
    }

    [Fact]
    public void ReplacementSnippetsUseDotNetSubstitutionSyntax()
    {
        var snippets = RegexSnippetCatalog.ReplacementCategories
            .SelectMany(category => category.Snippets)
            .ToDictionary(snippet => snippet.Label, snippet => snippet.Text);
        var request = new RegexRequest(
            @"(?<name>alpha)-(?<tail>beta)",
            "prefix alpha-beta suffix");
        var engine = new DotNetRegexEngine();

        AssertReplacement("Group N ($N)", "$§N§", "prefix alpha suffix");
        AssertReplacement("Named group (${name})", "${§name§}", "prefix alpha suffix");
        AssertReplacement("Entire match ($0)", "$0", "prefix alpha-beta suffix");
        AssertReplacement(
            "Last captured group ($+)",
            "$+",
            "prefix beta suffix");
        AssertReplacement(
            "Entire source string ($_)",
            "$_",
            "prefix prefix alpha-beta suffix suffix");
        AssertReplacement(
            "Source string before match ($`)",
            "$`",
            "prefix prefix  suffix");
        AssertReplacement(
            "Source string after match ($')",
            "$'",
            "prefix  suffix suffix");

        void AssertReplacement(string label, string template, string expected)
        {
            Assert.Equal(template, snippets[label]);
            var replacement = template
                .Replace("§N§", "1", StringComparison.Ordinal)
                .Replace("§name§", "name", StringComparison.Ordinal);

            var result = engine.Replace(request, replacement);

            Assert.Equal(RegexExecutionStatus.Success, result.Status);
            Assert.Equal(expected, result.Output);
        }
    }

    [Fact]
    public void ReplacementCharacterEscapesRemainTextualForDotNetReplace()
    {
        var snippets = Assert.Single(
            RegexSnippetCatalog.ReplacementCategories,
            category => category.Name == "Character escapes");
        var engine = new DotNetRegexEngine();
        var request = new RegexRequest("x", "x");

        foreach (var snippet in snippets.Snippets)
        {
            var inserted = SnippetTextInsertion.Apply(string.Empty, 0, 0, snippet.Text).Text;
            Assert.DoesNotContain(inserted, char.IsControl);

            var result = engine.Replace(request, inserted);

            Assert.Equal(RegexExecutionStatus.Success, result.Status);
            Assert.Equal(inserted, result.Output);
        }
    }

    private static string FillPlaceholders(string snippet, string label)
    {
        var placeholderValue = label.StartsWith("Unicode character class", StringComparison.Ordinal)
            ? "L"
            : "capture";

        return snippet
            .Replace("§characters§", "a-z", StringComparison.Ordinal)
            .Replace("§name§", placeholderValue, StringComparison.Ordinal)
            .Replace("§N§", "2", StringComparison.Ordinal)
            .Replace("§M§", "4", StringComparison.Ordinal)
            .Replace("§expression§", "a", StringComparison.Ordinal)
            .Replace("§first§", "a", StringComparison.Ordinal)
            .Replace("§second§", "b", StringComparison.Ordinal);
    }
}
