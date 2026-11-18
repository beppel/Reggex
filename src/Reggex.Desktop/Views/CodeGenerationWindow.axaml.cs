using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Reggex.Desktop.CodeGeneration;
using System.Text.RegularExpressions;

namespace Reggex.Desktop.Views;

public partial class CodeGenerationWindow : Window
{
    private readonly string _pattern = string.Empty;
    private readonly string _replacement = string.Empty;
    private readonly RegexOptions _regexOptions;
    private readonly RegexCodeSampleKind _sampleKind;
    private bool _allowClose;

    public CodeGenerationWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnDialogKeyDown, RoutingStrategies.Tunnel);
        Closing += OnClosing;
    }

    public CodeGenerationWindow(
        string pattern,
        string replacement,
        RegexOptions regexOptions,
        RegexCodeSampleKind sampleKind) : this()
    {
        _pattern = pattern;
        _replacement = replacement;
        _regexOptions = regexOptions;
        _sampleKind = sampleKind;
        OperationDescription.Text = sampleKind switch
        {
            RegexCodeSampleKind.Find => "Search for matches using the current expression and options.",
            RegexCodeSampleKind.Replace => "Replace matches using the current expression and replacement.",
            RegexCodeSampleKind.Split => "Split the current input using the expression as a separator.",
            _ => string.Empty,
        };
        GenerateCode();
    }

    private void OnOptionChanged(object? sender, RoutedEventArgs args) => GenerateCode();

    private void GenerateCode()
    {
        if (GeneratedCodeEditor is null)
        {
            return;
        }

        InstanceMethodOption.IsEnabled = GeneratedRegexOption.IsChecked != true;
        GenerateLoopOption.IsEnabled = _sampleKind == RegexCodeSampleKind.Find;
        if (!GenerateLoopOption.IsEnabled)
        {
            GenerateLoopOption.IsChecked = false;
        }
        GeneratedCodeEditor.Text = RegexCodeGenerator.Generate(new RegexCodeGenerationOptions(
            _pattern,
            _replacement,
            _regexOptions,
            _sampleKind,
            InstanceMethodOption.IsChecked == true,
            AssumeUsingOption.IsChecked == true,
            VerbatimStringsOption.IsChecked == true,
            GenerateLoopOption.IsChecked == true,
            GeneratedRegexOption.IsChecked == true));
    }

    private void OnCloseClick(object? sender, RoutedEventArgs args) => Close();

    private void OnDialogKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape && args.KeyModifiers == KeyModifiers.None)
        {
            args.Handled = true;
            Close();
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_allowClose || CopyOnExitOption.IsChecked != true)
        {
            return;
        }

        args.Cancel = true;
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetTextAsync(GeneratedCodeEditor.Text ?? string.Empty);
            }
        }
        catch
        {
            // Closing should still succeed if the platform clipboard is unavailable.
        }
        finally
        {
            _allowClose = true;
            Close();
        }
    }
}
