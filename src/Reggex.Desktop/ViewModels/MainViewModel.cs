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
    private bool _cultureInvariant;

    [ObservableProperty]
    private bool _rightToLeft;

    [ObservableProperty]
    private bool _ecmaScript;

    [ObservableProperty]
    private ImmutableArray<RegexMatchResult> _matches = [];

    [ObservableProperty]
    private int _selectedMatchIndex = -1;

    [ObservableProperty]
    private string _replacementOutput = string.Empty;

    [ObservableProperty]
    private ImmutableArray<SplitSegmentViewModel> _splitSegments = [];

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

    public bool IsSplitMode
    {
        get => SelectedModeIndex == 2;
        set
        {
            if (value)
            {
                SelectedModeIndex = 2;
            }
        }
    }

    public int MatchCount => Matches.Length;

    public bool HasNoMatches => !HasError && !IsEvaluating && Matches.IsEmpty;

    public bool HasMatches => !HasError && !IsEvaluating && !Matches.IsEmpty;

    public bool HasNoSplitSegments => !HasError && !IsEvaluating && SplitSegments.IsEmpty;

    public bool HasSplitSegments => !HasError && !IsEvaluating && !SplitSegments.IsEmpty;

    public bool HasErrorPosition => !string.IsNullOrEmpty(ErrorPosition);

    public string MatchCountText => MatchCount == 1 ? "1 match" : $"{MatchCount} matches";

    public string SplitSegmentCountText => SplitSegments.Length == 1
        ? "1 segment"
        : $"{SplitSegments.Length} segments";

    partial void OnPatternChanged(string value) => ScheduleEvaluation();
    partial void OnTestInputChanged(string value) => ScheduleEvaluation();
    partial void OnReplacementChanged(string value) => ScheduleEvaluation();
    partial void OnIgnoreCaseChanged(bool value) => ScheduleEvaluation();
    partial void OnMultilineChanged(bool value) => ScheduleEvaluation();
    partial void OnSinglelineChanged(bool value) => ScheduleEvaluation();
    partial void OnExplicitCaptureChanged(bool value) => ScheduleEvaluation();
    partial void OnIgnorePatternWhitespaceChanged(bool value) => ScheduleEvaluation();
    partial void OnCultureInvariantChanged(bool value) => ScheduleEvaluation();
    partial void OnRightToLeftChanged(bool value) => ScheduleEvaluation();
    partial void OnEcmaScriptChanged(bool value) => ScheduleEvaluation();

    partial void OnSelectedModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFindMode));
        OnPropertyChanged(nameof(IsReplaceMode));
        OnPropertyChanged(nameof(IsSplitMode));
        ScheduleEvaluation();
    }

    partial void OnMatchesChanged(ImmutableArray<RegexMatchResult> value)
    {
        OnPropertyChanged(nameof(MatchCount));
        OnPropertyChanged(nameof(MatchCountText));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(HasMatches));
    }

    partial void OnSplitSegmentsChanged(ImmutableArray<SplitSegmentViewModel> value)
    {
        OnPropertyChanged(nameof(HasNoSplitSegments));
        OnPropertyChanged(nameof(HasSplitSegments));
        OnPropertyChanged(nameof(SplitSegmentCountText));
    }

    partial void OnHasErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(HasMatches));
        OnPropertyChanged(nameof(HasNoSplitSegments));
        OnPropertyChanged(nameof(HasSplitSegments));
    }

    partial void OnIsEvaluatingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(HasMatches));
        OnPropertyChanged(nameof(HasNoSplitSegments));
        OnPropertyChanged(nameof(HasSplitSegments));
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
        var splitMode = IsSplitMode;

        IsEvaluating = true;
        SelectedMatchIndex = -1;
        HasError = false;
        ErrorMessage = string.Empty;
        ErrorPosition = string.Empty;

        _ = EvaluateAfterDelayAsync(pattern, input, replacement, options, replaceMode, splitMode, version, token);
    }

    private async Task EvaluateAfterDelayAsync(
        string pattern,
        string input,
        string replacement,
        RegexOptions options,
        bool replaceMode,
        bool splitMode,
        int version,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);

            var request = new RegexRequest(pattern, input, options);
            var evaluation = await Task.Run(() => Evaluate(request, replacement, replaceMode, splitMode), cancellationToken);

            if (cancellationToken.IsCancellationRequested || version != _evaluationVersion)
            {
                return;
            }

            Matches = evaluation.Result.Matches;
            ReplacementOutput = evaluation.ReplacementOutput;
            SplitSegments = evaluation.SplitSegments
                .Select((segment, index) => new SplitSegmentViewModel(index, segment))
                .ToImmutableArray();
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
            SplitSegments = [];
            HasError = true;
            ErrorMessage = $"The selected regex options are not supported together. {exception.Message}";
            ErrorPosition = string.Empty;
            IsEvaluating = false;
        }
    }

    private Evaluation Evaluate(RegexRequest request, string replacement, bool replaceMode, bool splitMode)
    {
        if (splitMode)
        {
            var splitResult = _regexEngine.Split(request);
            var splitExecutionResult = ToExecutionResult(splitResult.Status, splitResult.Error);
            return new Evaluation(splitExecutionResult, string.Empty, splitResult.Segments);
        }

        var result = _regexEngine.Execute(request);
        if (!replaceMode || result.Status != RegexExecutionStatus.Success)
        {
            return new Evaluation(result, string.Empty, []);
        }

        var replacementResult = _regexEngine.Replace(request, replacement);
        if (replacementResult.Status == RegexExecutionStatus.Success)
        {
            return new Evaluation(result, replacementResult.Output, []);
        }

        return new Evaluation(ToExecutionResult(replacementResult.Status, replacementResult.Error), string.Empty, []);
    }

    private static RegexExecutionResult ToExecutionResult(
        RegexExecutionStatus status,
        RegexExecutionError? error)
        => status switch
        {
            RegexExecutionStatus.Success => RegexExecutionResult.Succeeded([]),
            RegexExecutionStatus.InvalidPattern => RegexExecutionResult.InvalidPattern(
                error?.Details,
                error?.Position),
            RegexExecutionStatus.TimedOut => RegexExecutionResult.TimedOut(error?.Details),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown regex execution status.")
        };

    private RegexOptions GetRegexOptions()
    {
        var options = RegexOptions.None;
        if (IgnoreCase) options |= RegexOptions.IgnoreCase;
        if (Multiline) options |= RegexOptions.Multiline;
        if (Singleline) options |= RegexOptions.Singleline;
        if (ExplicitCapture) options |= RegexOptions.ExplicitCapture;
        if (IgnorePatternWhitespace) options |= RegexOptions.IgnorePatternWhitespace;
        if (CultureInvariant) options |= RegexOptions.CultureInvariant;
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

    public void SelectMatch(RegexMatchResult match)
    {
        ArgumentNullException.ThrowIfNull(match);
        SelectedMatchIndex = Matches.IndexOf(match);
    }

    private sealed record Evaluation(
        RegexExecutionResult Result,
        string ReplacementOutput,
        ImmutableArray<string> SplitSegments);
}

public sealed record SplitSegmentViewModel(int Index, string Value)
{
    public string IndexLabel => $"[{Index,3}]:";
}
