using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace Reggex.Desktop.Views;

public partial class AboutWindow : Window
{
    private const string RepositoryUrl = "https://github.com/beppel/Reggex";

    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs args) => Close();

    private void OnRepositoryClick(object? sender, RoutedEventArgs args)
    {
        Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
    }
}
