using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Reggex.Core;
using Reggex.Desktop.ViewModels;
using Reggex.Desktop.Views;

namespace Reggex.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(new DotNetRegexEngine()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
