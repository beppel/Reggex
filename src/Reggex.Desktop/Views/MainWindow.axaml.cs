using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using Reggex.Desktop.ViewModels;
using Reggex.Core;
using System.ComponentModel;

namespace Reggex.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _subscribedViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            _subscribedViewModel = DataContext as MainViewModel;
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            UpdatePreview();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainViewModel.PreviewSegments))
        {
            UpdatePreview();
        }
        else if (args.PropertyName == nameof(MainViewModel.SelectedMatchIndex))
        {
            ScrollPreviewToSelectedMatch();
        }
    }

    private void OnMatchPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs args)
    {
        if (sender is Border { DataContext: RegexMatchResult match } && DataContext is MainViewModel viewModel)
        {
            viewModel.SelectMatch(match);
        }
    }

    private void UpdatePreview()
    {
        if (DataContext is not MainViewModel viewModel || this.FindControl<TextBlock>("HighlightedPreview") is not { } preview)
        {
            return;
        }

        var inlines = preview.Inlines;
        if (inlines is null)
        {
            return;
        }

        inlines.Clear();
        foreach (var segment in viewModel.PreviewSegments)
        {
            var run = new Run(segment.Text);
            if (segment.IsHighlighted)
            {
                run.Background = Brush.Parse(segment.IsSelected || segment.IsPositionMarker ? "#D99735" : "#365B8C");
                run.Foreground = Brush.Parse("#F3F7FC");
            }
            inlines.Add(run);
        }

        ScrollPreviewToSelectedMatch();
    }

    private void ScrollPreviewToSelectedMatch()
    {
        if (DataContext is not MainViewModel { SelectedMatchIndex: >= 0 } viewModel ||
            this.FindControl<ScrollViewer>("PreviewScrollViewer") is not { } scrollViewer ||
            viewModel.SelectedMatchIndex >= viewModel.Matches.Length)
        {
            return;
        }

        var start = viewModel.Matches[viewModel.SelectedMatchIndex].Index;
        var line = 0;
        for (var i = 0; i < Math.Min(start, viewModel.TestInput.Length); i++)
        {
            if (viewModel.TestInput[i] == '\n') line++;
        }

        scrollViewer.Offset = new Avalonia.Vector(scrollViewer.Offset.X, Math.Max(0, line * 20d));
    }
}
