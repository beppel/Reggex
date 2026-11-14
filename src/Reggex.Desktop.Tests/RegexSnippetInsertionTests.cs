using Reggex.Desktop.Snippets;

namespace Reggex.Desktop.Tests;

public class RegexSnippetInsertionTests
{
    [Theory]
    [InlineData("abcd", 2, 2, "X", "abXcd", 3)]
    [InlineData("abcd", 1, 3, "X", "aXd", 2)]
    [InlineData("abcd", 3, 1, "X", "aXd", 2)]
    [InlineData("abcd", 1, 3, "", "ad", 1)]
    public void ApplyInsertsAtCaretOrReplacesSelection(
        string text,
        int selectionStart,
        int selectionEnd,
        string snippet,
        string expectedText,
        int expectedCaretIndex)
    {
        var result = SnippetTextInsertion.Apply(text, selectionStart, selectionEnd, snippet);

        Assert.Equal(expectedText, result.Text);
        Assert.Equal(expectedCaretIndex, result.SelectionStart);
        Assert.Equal(0, result.SelectionLength);
    }

    [Fact]
    public void ApplyClampsSelectionToTextBounds()
    {
        var result = SnippetTextInsertion.Apply("abc", -4, 8, "X");

        Assert.Equal("X", result.Text);
        Assert.Equal(1, result.SelectionStart);
        Assert.Equal(0, result.SelectionLength);
    }

    [Fact]
    public void ApplyRemovesMarkersAndSelectsTheFirstPlaceholder()
    {
        var result = SnippetTextInsertion.Apply("abc", 1, 1, @"\d{§N§,§M§}");

        Assert.Equal(@"a\d{N,M}bc", result.Text);
        Assert.Equal(4, result.SelectionStart);
        Assert.Equal(1, result.SelectionLength);
        Assert.Equal(5, result.SelectionEnd);
    }

    [Fact]
    public void ApplyRemovesAllMarkersAndPlacesTheCaretAtTheEndWhenThereAreNoPlaceholders()
    {
        var result = SnippetTextInsertion.Apply("ab", 1, 1, @"\b");

        Assert.Equal(@"a\bb", result.Text);
        Assert.Equal(3, result.SelectionStart);
        Assert.Equal(0, result.SelectionLength);
    }
}
