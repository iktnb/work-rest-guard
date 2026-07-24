namespace WorkRestGuard.Tests;

public sealed class CountdownFormatterTests
{
    [Fact]
    public void WorkCountdownIncludesHoursAndRoundsUp()
    {
        var now = new DateTimeOffset(2026, 7, 24, 8, 0, 0, TimeSpan.Zero);

        var formatted = CountdownFormatter.FormatRemaining(
            now.AddMinutes(90).AddMilliseconds(-100),
            now);

        Assert.Equal("01:30:00", formatted);
    }

    [Fact]
    public void ShortCountdownUsesMinutesAndNeverGoesNegative()
    {
        var now = new DateTimeOffset(2026, 7, 24, 8, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            "14:59",
            CountdownFormatter.FormatRemaining(now.AddMinutes(15).AddSeconds(-1), now));
        Assert.Equal(
            "00:00",
            CountdownFormatter.FormatRemaining(now.AddSeconds(-1), now));
    }

    [Fact]
    public void MissingDeadlineHasStablePlaceholder()
    {
        Assert.Equal(
            "--:--",
            CountdownFormatter.FormatRemaining(null, DateTimeOffset.UtcNow));
    }
}
