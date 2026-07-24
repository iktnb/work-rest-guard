namespace WorkRestGuard.Tests;

public sealed class ExactTypingMatcherTests
{
    [Fact]
    public void ExactTextMatches()
    {
        Assert.True(ExactTypingMatcher.IsExactMatch("символ у символ", "символ у символ"));
        Assert.Null(ExactTypingMatcher.FindFirstMismatch("abc", "abc"));
    }

    [Fact]
    public void ComparisonIsCaseAndPunctuationSensitive()
    {
        Assert.Equal(0, ExactTypingMatcher.FindFirstMismatch("Abc.", "abc."));
        Assert.Equal(3, ExactTypingMatcher.FindFirstMismatch("abc.", "abc,"));
        Assert.False(ExactTypingMatcher.IsExactMatch("abc.", "abc,"));
    }

    [Fact]
    public void ExtraTextIsReportedAtExpectedLength()
    {
        Assert.Equal(3, ExactTypingMatcher.FindFirstMismatch("abc", "abcd"));
    }

    [Fact]
    public void ApprovedStatementsContainNoLineBreaks()
    {
        Assert.DoesNotContain('\n', UiText.DayStatementUk);
        Assert.DoesNotContain('\r', UiText.NightStatementUk);
        Assert.True(UiText.DayStatementUk.Length > 500);
        Assert.True(UiText.NightStatementUk.Length > 500);
    }
}
