using System.Collections.Immutable;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Reggex.Core;

namespace Reggex.Desktop.Views;

public sealed class RegexMatchHighlighter : DocumentColorizingTransformer, IBackgroundRenderer
{
    private static readonly IBrush MatchBrush = Brush.Parse("#365B8C");
    private static readonly IBrush SelectedMatchBrush = Brush.Parse("#D99735");

    private ImmutableArray<RegexMatchResult> _matches = [];
    private RegexMatchResult? _selectedMatch;
    private bool _matchesAreOrderedByIndex = true;
    private bool _matchesInAscendingOrder = true;

    public void SetMatches(ImmutableArray<RegexMatchResult> matches, int selectedMatchIndex)
    {
        _matches = matches;
        _selectedMatch = selectedMatchIndex >= 0 && selectedMatchIndex < matches.Length
            ? matches[selectedMatchIndex]
            : null;

        var ascending = true;
        var descending = true;
        for (var i = 1; i < matches.Length; i++)
        {
            var previous = matches[i - 1];
            var current = matches[i];
            var previousEnd = (long)previous.Index + previous.Length;
            var currentEnd = (long)current.Index + current.Length;

            if (previous.Index > current.Index || previousEnd > currentEnd)
            {
                ascending = false;
            }

            if (previous.Index < current.Index || previousEnd < currentEnd)
            {
                descending = false;
            }
        }

        _matchesAreOrderedByIndex = ascending || descending;
        _matchesInAscendingOrder = ascending;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        foreach (var match in _matches)
        {
            if (match.Length != 0) continue;

            var brush = ReferenceEquals(match, _selectedMatch) ? SelectedMatchBrush : MatchBrush;
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, new ZeroLengthSegment(match.Index)))
            {
                const double markerWidth = 2;
                drawingContext.DrawRectangle(
                    brush,
                    null,
                    new Rect(rect.Left - markerWidth / 2, rect.Top, markerWidth, rect.Height));
            }
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var lineStart = line.Offset;
        var lineEnd = line.Offset + line.Length;

        if (_matchesAreOrderedByIndex)
        {
            var firstMatch = FindFirstMatchForLine(lineStart, lineEnd);
            for (var i = firstMatch; i < _matches.Length; i++)
            {
                var match = _matches[i];
                if (_matchesInAscendingOrder
                    ? match.Index >= lineEnd
                    : (long)match.Index + match.Length <= lineStart)
                {
                    break;
                }

                if (match.Length > 0)
                {
                    ApplyRange(match.Index, match.Index + match.Length, lineStart, lineEnd, MatchBrush);
                }
            }
        }
        else
        {
            foreach (var match in _matches)
            {
                if (match.Length > 0)
                {
                    ApplyRange(match.Index, match.Index + match.Length, lineStart, lineEnd, MatchBrush);
                }
            }
        }

        if (_selectedMatch is { Length: > 0 } selected)
        {
            ApplyRange(selected.Index, selected.Index + selected.Length, lineStart, lineEnd, SelectedMatchBrush);
        }
    }

    private int FindFirstMatchForLine(int lineStart, int lineEnd)
    {
        var low = 0;
        var high = _matches.Length;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            var match = _matches[middle];
            var isBeforeLine = _matchesInAscendingOrder
                ? (long)match.Index + match.Length <= lineStart
                : match.Index >= lineEnd;

            if (isBeforeLine)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private void ApplyRange(int start, int end, int lineStart, int lineEnd, IBrush brush)
    {
        var clippedStart = Math.Max(start, lineStart);
        var clippedEnd = Math.Min(end, lineEnd);
        if (clippedStart < clippedEnd)
        {
            ChangeLinePart(clippedStart, clippedEnd, element => element.BackgroundBrush = brush);
        }
    }

    private readonly record struct ZeroLengthSegment(int Offset) : ISegment
    {
        public int Length => 0;
        public int EndOffset => Offset;
    }
}
