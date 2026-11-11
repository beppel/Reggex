using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using Reggex.Desktop.ViewModels;
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
                run.Background = Brush.Parse(segment.IsPositionMarker ? "#D99735" : "#365B8C");
                run.Foreground = Brush.Parse("#F3F7FC");
            }
            inlines.Add(run);
        }
    }
}
