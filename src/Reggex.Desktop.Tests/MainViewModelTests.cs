using Reggex.Core;
using Reggex.Desktop.ViewModels;

namespace Reggex.Desktop.Tests;

public class MainViewModelTests
{
    private readonly DotNetRegexEngine _engine = new();

    [Fact]
    public async Task ScheduleEvaluationRapidEditsDoNotThrow()
    {
        using var viewModel = new MainViewModel(_engine);

        // Rapidly change properties in a tight loop to simulate fast typing
        for (var i = 0; i < 50; i++)
        {
            viewModel.Pattern = $"word_{i}";
            viewModel.TestInput = $"sample text with word_{i} inside";
        }

        // Wait for the debounce delay to expire and background task to finish
        await Task.Delay(350);

        Assert.False(viewModel.IsEvaluating);
        Assert.False(viewModel.HasError);
        Assert.Single(viewModel.Matches);
        Assert.Equal("word_49", viewModel.Matches[0].Value);
    }

    [Fact]
    public async Task MainViewModelEvaluatesMatchesAfterDelay()
    {
        using var viewModel = new MainViewModel(_engine)
        {
            Pattern = @"\d+",
            TestInput = "12 34 56"
        };

        await Task.Delay(300);

        Assert.False(viewModel.IsEvaluating);
        Assert.False(viewModel.HasError);
        Assert.True(viewModel.HasMatches);
        Assert.False(viewModel.HasNoMatches);
        Assert.Equal(3, viewModel.MatchCount);
        Assert.Equal("3 matches", viewModel.MatchCountText);
    }

    [Fact]
    public async Task MainViewModelReportsInvalidPattern()
    {
        using var viewModel = new MainViewModel(_engine)
        {
            Pattern = "("
        };

        await Task.Delay(300);

        Assert.False(viewModel.IsEvaluating);
        Assert.True(viewModel.HasError);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
        Assert.True(viewModel.HasErrorPosition);
    }

    [Fact]
    public async Task MainViewModelReportsIncompatibleOptions()
    {
        using var viewModel = new MainViewModel(_engine)
        {
            Pattern = "abc",
            EcmaScript = true,
            ExplicitCapture = true
        };

        await Task.Delay(300);

        Assert.False(viewModel.IsEvaluating);
        Assert.True(viewModel.HasError);
        Assert.Contains("The selected regex options are not supported together", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task MainViewModelUpdatesModeProperties()
    {
        using var viewModel = new MainViewModel(_engine);

        Assert.True(viewModel.IsFindMode);
        Assert.False(viewModel.IsReplaceMode);
        Assert.False(viewModel.IsSplitMode);

        viewModel.IsReplaceMode = true;
        Assert.False(viewModel.IsFindMode);
        Assert.True(viewModel.IsReplaceMode);
        Assert.False(viewModel.IsSplitMode);

        viewModel.IsSplitMode = true;
        Assert.False(viewModel.IsFindMode);
        Assert.False(viewModel.IsReplaceMode);
        Assert.True(viewModel.IsSplitMode);
    }

    [Fact]
    public void MainViewModelDisposesSafely()
    {
        var viewModel = new MainViewModel(_engine);
        viewModel.Dispose();
        // Calling dispose again should not throw
        viewModel.Dispose();
    }
}
