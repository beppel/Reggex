using Avalonia.Controls;
using AvaloniaEdit;
using Reggex.Desktop.ViewModels;
using Reggex.Core;
using AvaloniaEdit.Rendering;
using System.ComponentModel;

namespace Reggex.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _subscribedViewModel;
    private RegexMatchHighlighter? _matchHighlighter;
    private TextEditor? _sourceEditor;

    public MainWindow()
    {
        InitializeComponent();
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
