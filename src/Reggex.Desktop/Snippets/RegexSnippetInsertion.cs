using System.Text;

namespace Reggex.Desktop.Snippets;

public static class SnippetTextInsertion
{
    public static SnippetInsertionResult Apply(
        string text,
        int selectionStart,
        int selectionEnd,
        string snippet)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(snippet);

        var cleanSnippetBuilder = new StringBuilder(snippet.Length);
        var markerPositions = new List<int>();
        foreach (var character in snippet)
        {
            if (character == '§')
            {
                markerPositions.Add(cleanSnippetBuilder.Length);
            }
            else
            {
                cleanSnippetBuilder.Append(character);
            }
        }

        if (markerPositions.Count % 2 != 0)
        {
            throw new ArgumentException("Snippet placeholder markers must be paired.", nameof(snippet));
        }

        var cleanSnippet = cleanSnippetBuilder.ToString();
        var start = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(selectionStart, selectionEnd), start, text.Length);
        var insertedText = text[..start] + cleanSnippet + text[end..];
        var placeholderStart = markerPositions.Count >= 2
            ? start + markerPositions[0]
            : start + cleanSnippet.Length;
        var placeholderLength = markerPositions.Count >= 2
            ? markerPositions[1] - markerPositions[0]
            : 0;

        return new SnippetInsertionResult(insertedText, placeholderStart, placeholderLength);
    }
}

public sealed record SnippetInsertionResult(string Text, int SelectionStart, int SelectionLength)
{
    public int SelectionEnd => SelectionStart + SelectionLength;
}
