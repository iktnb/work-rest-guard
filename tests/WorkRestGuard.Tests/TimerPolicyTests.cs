namespace WorkRestGuard.Tests;

public sealed class TimerPolicyTests
{
    [Theory]
    [InlineData(19, 59, false)]
    [InlineData(20, 0, true)]
    [InlineData(23, 59, true)]
    [InlineData(0, 0, true)]
    [InlineData(5, 59, true)]
    [InlineData(6, 0, false)]
    public void QuietHoursHideOnlyTheTimer(int hour, int minute, bool quiet)
    {
        var config = TestData.Config();
        var now = new DateTimeOffset(2026, 10, 19, hour, minute, 0, TimeSpan.Zero);
        Assert.Equal(quiet, TimerPolicy.IsQuietTime(now, config, TimeZoneInfo.Utc));
        var engine = new GuardEngine(config, new(), new FakeClock(now));
        engine.StartManualBreak();
        Assert.Equal(!quiet, TimerPolicy.ShouldShowTimer(now, config, engine.View, TimeZoneInfo.Utc));
        Assert.False(engine.View.ShowOverlay);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    [InlineData(12, false)]
    public void NonCrossingWindowRespectsBoundaries(int hour, bool expected)
    {
        var config = TestData.Config();
        config.NightStart = new(10, 0);
        config.NightEnd = new(12, 0);
        Assert.Equal(expected, TimerPolicy.IsQuietTime(new(2026, 10, 19, hour, 0, 0, TimeSpan.Zero), config, TimeZoneInfo.Utc));
    }

    [Fact]
    public void UsesLocalTimeAndReappearsAfterQuietHours()
    {
        var config = TestData.Config();
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test+03", TimeSpan.FromHours(3), "Test", "Test");
        var now = new DateTimeOffset(2026, 10, 19, 17, 0, 0, TimeSpan.Zero);
        var view = new GuardViewState(GuardPhase.DayResting, false, null, now.AddDays(1), false, false, null);
        Assert.False(TimerPolicy.ShouldShowTimer(now, config, view, zone));
        Assert.True(TimerPolicy.ShouldShowTimer(now.AddHours(10), config, view, zone));
        config.GuardEnabled = false;
        Assert.False(TimerPolicy.ShouldShowTimer(now.AddHours(10), config, view, zone));
    }

    [Fact]
    public void InvalidLegacyEqualTimesRecoverToDefaults()
    {
        var config = TestData.Config();
        config.NightStart = config.NightEnd;
        config.Normalize();
        Assert.Equal(new TimeOnly(20, 0), config.NightStart);
        Assert.Equal(new TimeOnly(6, 0), config.NightEnd);
    }
}
