namespace WorkRestGuard.Tests;

public sealed class ScheduleResolverTests
{
    private readonly ScheduleResolver _resolver = new(TimeZoneInfo.Utc);

    [Fact]
    public void CrossMidnightWindowUsesStartDateAsIdentity()
    {
        var config = TestData.Config();
        var mondayEvening = new DateTimeOffset(2026, 7, 20, 21, 0, 0, TimeSpan.Zero);
        var tuesdayMorning = new DateTimeOffset(2026, 7, 21, 2, 0, 0, TimeSpan.Zero);

        var eveningWindow = _resolver.GetActiveNightWindow(mondayEvening, config);
        var morningWindow = _resolver.GetActiveNightWindow(tuesdayMorning, config);

        Assert.NotNull(eveningWindow);
        Assert.NotNull(morningWindow);
        Assert.Equal("2026-07-20", eveningWindow.Id);
        Assert.Equal(eveningWindow, morningWindow);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 21, 6, 0, 0, TimeSpan.Zero),
            eveningWindow.EndsAtUtc);
    }

    [Fact]
    public void WindowEndsExactlyAtConfiguredEnd()
    {
        var config = TestData.Config();
        var atEnd = new DateTimeOffset(2026, 7, 21, 6, 0, 0, TimeSpan.Zero);

        Assert.Null(_resolver.GetActiveNightWindow(atEnd, config));
    }

    [Fact]
    public void DisabledStartDayDoesNotLeakAfterMidnight()
    {
        var config = TestData.Config();
        config.NightDays = [DayOfWeek.Tuesday];
        var tuesdayAtTwo = new DateTimeOffset(2026, 7, 21, 2, 0, 0, TimeSpan.Zero);
        var tuesdayAtTwentyOne = new DateTimeOffset(2026, 7, 21, 21, 0, 0, TimeSpan.Zero);

        Assert.Null(_resolver.GetActiveNightWindow(tuesdayAtTwo, config));
        Assert.NotNull(_resolver.GetActiveNightWindow(tuesdayAtTwentyOne, config));
    }

    [Fact]
    public void SameDayWindowIsSupported()
    {
        var config = TestData.Config();
        config.NightStart = new TimeOnly(10, 0);
        config.NightEnd = new TimeOnly(12, 0);

        Assert.NotNull(_resolver.GetActiveNightWindow(
            new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero),
            config));
        Assert.Null(_resolver.GetActiveNightWindow(
            new DateTimeOffset(2026, 7, 20, 12, 0, 0, TimeSpan.Zero),
            config));
    }
}
