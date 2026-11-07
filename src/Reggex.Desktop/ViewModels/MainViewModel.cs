using System.Collections.Immutable;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Reggex.Core;

namespace Reggex.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IRegexEngine _regexEngine;
    private CancellationTokenSource? _evaluationCancellation;
    private int _evaluationVersion;

    [ObservableProperty]
    private string _pattern = @"(?<word>\b\w+\b)";

    [ObservableProperty]
    private string _testInput = "Try a pattern against this sample text.";

    [ObservableProperty]
    private string _replacement = "<${word}>";

    [ObservableProperty]
    private int _selectedModeIndex;

    [ObservableProperty]
    private bool _ignoreCase;

    [ObservableProperty]
    private bool _multiline;

    [ObservableProperty]
    private bool _singleline;

    [ObservableProperty]
    private bool _explicitCapture;

    [ObservableProperty]
    private bool _ignorePatternWhitespace;

    [ObservableProperty]
    private bool _rightToLeft;

    [ObservableProperty]
    private bool _ecmaScript;

    [ObservableProperty]
    private ImmutableArray<RegexMatchResult> _matches = [];

    [ObservableProperty]
    private string _replacementOutput = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _errorPosition = string.Empty;

    [ObservableProperty]
    private bool _isEvaluating;

    public MainViewModel(IRegexEngine regexEngine)
    {
        ArgumentNullException.ThrowIfNull(regexEngine);
        _regexEngine = regexEngine;
        ScheduleEvaluation();
    }

    public bool IsFindMode
    {
        get => SelectedModeIndex == 0;
        set
        {
            if (value)
            {
                SelectedModeIndex = 0;
            }
        }
    }

    public bool IsReplaceMode
    {
        get => SelectedModeIndex == 1;
        set
        {
            if (value)
            {
                SelectedModeIndex = 1;
            }
        }
    }

    public int MatchCount => Matches.Length;

    public bool HasNoMatches => !HasError && !IsEvaluating && Matches.IsEmpty;

    public bool HasMatches => !HasError && !IsEvaluating && !Matches.IsEmpty;

    public bool HasErrorPosition => !string.IsNullOrEmpty(ErrorPosition);

    public string MatchCountText => MatchCount == 1 ? "1 match" : $"{MatchCount} matches";

    partial void OnPatternChanged(string value) => ScheduleEvaluation();
    partial void OnTestInputChanged(string value) => ScheduleEvaluation();
    partial void OnReplacementChanged(string value) => ScheduleEvaluation();
    partial void OnIgnoreCaseChanged(bool value) => ScheduleEvaluation();
    partial void OnMultilineChanged(bool value) => ScheduleEvaluation();
    partial void OnSinglelineChanged(bool value) => ScheduleEvaluation();
    partial void OnExplicitCaptureChanged(bool value) => ScheduleEvaluation();
    partial void OnIgnorePatternWhitespaceChanged(bool value) => ScheduleEvaluation();
    partial void OnRightToLeftChanged(bool value) => ScheduleEvaluation();
    partial void OnEcmaScriptChanged(bool value) => ScheduleEvaluation();

    partial void OnSelectedModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFindMode));
        OnPropertyChanged(nameof(IsReplaceMode));
        ScheduleEvaluation();
    }

    partial void OnMatchesChanged(ImmutableArray<RegexMatchResult> value)
    {
        OnPropertyChanged(nameof(MatchCount));
        OnPropertyChanged(nameof(MatchCountText));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(HasMatches));
    }

    partial void OnHasErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(HasMatches));
    }

    partial void OnIsEvaluatingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(HasMatches));
    }

    partial void OnErrorPositionChanged(string value) => OnPropertyChanged(nameof(HasErrorPosition));

    private void ScheduleEvaluation()
    {
        _evaluationCancellation?.Cancel();
        _evaluationCancellation?.Dispose();

        var cancellation = new CancellationTokenSource();
        _evaluationCancellation = cancellation;
        var version = ++_evaluationVersion;
        var token = cancellation.Token;
        var pattern = Pattern;
        var input = TestInput;
        var replacement = Replacement;
        var options = GetRegexOptions();
        var replaceMode = IsReplaceMode;

        IsEvaluating = true;
        HasError = false;
        ErrorMessage = string.Empty;
        ErrorPosition = string.Empty;

        _ = EvaluateAfterDelayAsync(pattern, input, replacement, options, replaceMode, version, token);
    }

    private async Task EvaluateAfterDelayAsync(
        string pattern,
        string input,
        string replacement,
        RegexOptions options,
        bool replaceMode,
        int version,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);

            var request = new RegexRequest(pattern, input, options);
            var evaluation = await Task.Run(() => Evaluate(request, replacement, replaceMode), cancellationToken);

            if (cancellationToken.IsCancellationRequested || version != _evaluationVersion)
            {
                return;
            }

            Matches = evaluation.Result.Matches;
            ReplacementOutput = evaluation.ReplacementOutput;
            HasError = evaluation.Result.Status != RegexExecutionStatus.Success;
            ErrorMessage = GetErrorMessage(evaluation.Result);
            ErrorPosition = evaluation.Result.Error?.Position is int position
                ? $"at position {position}"
                : string.Empty;
            IsEvaluating = false;
        }
        catch (OperationCanceledException)
        {
            // A newer edit superseded this evaluation.
        }
        catch (ArgumentOutOfRangeException exception)
        {
            if (version != _evaluationVersion)
            {
                return;
            }

            Matches = [];
            ReplacementOutput = string.Empty;
            HasError = true;
            ErrorMessage = $"The selected regex options are not supported together. {exception.Message}";
            ErrorPosition = string.Empty;
            IsEvaluating = false;
        }
    }

    private Evaluation Evaluate(RegexRequest request, string replacement, bool replaceMode)
    {
        var result = _regexEngine.Execute(request);
        if (!replaceMode || result.Status != RegexExecutionStatus.Success)
        {
            return new Evaluation(result, string.Empty);
        }

        var replacementResult = _regexEngine.Replace(request, replacement);
        if (replacementResult.Status == RegexExecutionStatus.Success)
        {
            return new Evaluation(result, replacementResult.Output);
        }

        var failedResult = replacementResult.Status switch
        {
            RegexExecutionStatus.InvalidPattern => RegexExecutionResult.InvalidPattern(
                replacementResult.Error?.Details,
                replacementResult.Error?.Position),
            RegexExecutionStatus.TimedOut => RegexExecutionResult.TimedOut(replacementResult.Error?.Details),
            _ => result
        };

        return new Evaluation(failedResult, string.Empty);
    }

    private RegexOptions GetRegexOptions()
    {
        var options = RegexOptions.None;
        if (IgnoreCase) options |= RegexOptions.IgnoreCase;
        if (Multiline) options |= RegexOptions.Multiline;
        if (Singleline) options |= RegexOptions.Singleline;
        if (ExplicitCapture) options |= RegexOptions.ExplicitCapture;
        if (IgnorePatternWhitespace) options |= RegexOptions.IgnorePatternWhitespace;
        if (RightToLeft) options |= RegexOptions.RightToLeft;
        if (EcmaScript) options |= RegexOptions.ECMAScript;
        return options;
    }

    private static string GetErrorMessage(RegexExecutionResult result)
        => result.Status switch
        {
            RegexExecutionStatus.InvalidPattern => result.Error?.Details ?? "The regular expression is invalid.",
            RegexExecutionStatus.TimedOut => "Regex execution timed out. " + result.Error?.Details,
            _ => string.Empty
        };

    private sealed record Evaluation(RegexExecutionResult Result, string ReplacementOutput);
}
