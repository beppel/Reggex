using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using System.Collections.Immutable;
using System.ComponentModel;
using Reggex.Core;
using Reggex.Desktop.Snippets;
using Reggex.Desktop.ViewModels;

namespace Reggex.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _subscribedViewModel;
    private RegexMatchHighlighter? _matchHighlighter;
    private TextEditor? _sourceEditor;

    public MainWindow()
    {
        InitializeComponent();
        AttachSnippetMenu(this.FindControl<TextBox>("FindPatternEditor"), RegexSnippetCatalog.PatternCategories);
        AttachSnippetMenu(this.FindControl<TextBox>("ReplacePatternEditor"), RegexSnippetCatalog.PatternCategories);
        AttachSnippetMenu(this.FindControl<TextBox>("SplitPatternEditor"), RegexSnippetCatalog.PatternCategories);
        AttachSnippetMenu(this.FindControl<TextBox>("ReplacementEditor"), RegexSnippetCatalog.ReplacementCategories);

        DataContextChanged += (_, _) =>
        {
            _sourceEditor = this.FindControl<TextEditor>("SourceEditor");
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            _subscribedViewModel = DataContext as MainViewModel;
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateSourceEditorText();
            }
            AttachMatchHighlighter();
            UpdateSourceHighlights();
        };
    }

    private static void AttachSnippetMenu(
        TextBox? textBox,
        ImmutableArray<RegexSnippetCategory> categories)
    {
        if (textBox is null)
        {
            return;
        }

        var selectionStart = 0;
        var selectionEnd = 0;
        var contextMenu = new ContextMenu();
        foreach (var category in categories)
        {
            var categoryItem = new MenuItem { Header = category.Name };
            foreach (var snippet in category.Snippets)
            {
                var snippetItem = new MenuItem { Header = snippet.Label };
                if (!string.IsNullOrWhiteSpace(snippet.ToolTip))
                {
                    ToolTip.SetTip(snippetItem, snippet.ToolTip);
                }

                snippetItem.Click += (_, _) =>
                    InsertSnippet(textBox, selectionStart, selectionEnd, snippet.Text);
                categoryItem.Items.Add(snippetItem);
            }

            contextMenu.Items.Add(categoryItem);
        }

        contextMenu.Opening += (_, _) =>
        {
            selectionStart = textBox.SelectionStart;
            selectionEnd = textBox.SelectionEnd;
        };
        textBox.ContextMenu = contextMenu;
    }

    private static void InsertSnippet(TextBox textBox, int selectionStart, int selectionEnd, string snippet)
    {
        var text = textBox.Text ?? string.Empty;
        var insertion = SnippetTextInsertion.Apply(text, selectionStart, selectionEnd, snippet);
        textBox.Text = insertion.Text;
        textBox.Focus();
        textBox.SelectionStart = insertion.SelectionStart;
        textBox.SelectionEnd = insertion.SelectionEnd;
        if (insertion.SelectionLength == 0)
        {
            textBox.CaretIndex = insertion.SelectionStart;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.TestInput))
        {
            UpdateSourceEditorText();
        }
        else if (args.PropertyName == nameof(MainViewModel.Matches) ||
            args.PropertyName == nameof(MainViewModel.SelectedMatchIndex) ||
            args.PropertyName == nameof(MainViewModel.IsEvaluating))
        {
            UpdateSourceHighlights();
        }
    }

    private void OnSourceTextChanged(object? sender, EventArgs args)
    {
        if (_sourceEditor is not null && DataContext is MainViewModel viewModel &&
            !string.Equals(viewModel.TestInput, _sourceEditor.Text, StringComparison.Ordinal))
        {
            viewModel.TestInput = _sourceEditor.Text;
        }
    }

    private void OnMatchPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs args)
    {
        if (sender is Border { DataContext: RegexMatchResult match } && DataContext is MainViewModel viewModel)
        {
            viewModel.SelectMatch(match);
            if (_sourceEditor is not null && match.Index >= 0 && match.Index <= _sourceEditor.Document.TextLength)
            {
                _sourceEditor.TextArea.Caret.Offset = match.Index;
            }
        }
    }

    private void AttachMatchHighlighter()
    {
        if (_sourceEditor is null || _matchHighlighter is not null)
        {
            return;
        }

        _matchHighlighter = new RegexMatchHighlighter();
        _sourceEditor.TextArea.TextView.LineTransformers.Add(_matchHighlighter);
        _sourceEditor.TextArea.TextView.BackgroundRenderers.Add(_matchHighlighter);
    }

    private void UpdateSourceHighlights()
    {
        if (DataContext is not MainViewModel viewModel || _sourceEditor is null || _matchHighlighter is null)
        {
            return;
        }

        var matches = viewModel.IsEvaluating ? [] : viewModel.Matches;
        _matchHighlighter.SetMatches(matches, viewModel.SelectedMatchIndex);
        _sourceEditor.TextArea.TextView.Redraw();
        _sourceEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
    }

    private void UpdateSourceEditorText()
    {
        if (_sourceEditor is not null && DataContext is MainViewModel viewModel &&
            !string.Equals(_sourceEditor.Text, viewModel.TestInput, StringComparison.Ordinal))
        {
            _sourceEditor.Text = viewModel.TestInput;
        }
    }
}
