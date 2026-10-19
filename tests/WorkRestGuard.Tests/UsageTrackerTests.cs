namespace WorkRestGuard.Tests;

public sealed class UsageTrackerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SwitchingAppsAttributesEachIntervalOnceToItsPreviousObservation()
    {
        var tracker = new UsageTracker();
        Assert.Null(tracker.Observe(At(0, UsageKind.Active, "code.exe")));
        var first = tracker.Observe(At(5, UsageKind.Active, "chrome.exe"));
        var second = tracker.Observe(At(10, UsageKind.Idle));
        var third = tracker.Observe(At(15, UsageKind.Locked));
        Assert.Equal("code.exe", first!.Application);
        Assert.Equal("chrome.exe", second!.Application);
        Assert.Equal(UsageKind.Idle, third!.Kind);
        Assert.Null(third.Application);
        Assert.Equal(TimeSpan.FromSeconds(5), first.Duration);
        Assert.Equal(first.EndUtc, second.StartUtc);
    }

    [Theory]
    [InlineData(16, 16000)]
    [InlineData(3600, 5000)]
    [InlineData(-5, 5000)]
    [InlineData(5, -5000)]
    public void MissingPollsAndClockDiscontinuitiesAreNotUsage(int seconds, int ticks)
    {
        var tracker = new UsageTracker();
        tracker.Observe(At(0, UsageKind.Active, "code.exe"));
        Assert.Null(tracker.Observe(new UsageObservation(Start.AddSeconds(seconds), ticks,
            UsageKind.Active, "chrome.exe", GuardPhase.Working, false)));
    }

    [Fact]
    public void ExplicitSuspendCanBridgeSleepButResetCannotBridgeOfflineTime()
    {
        var tracker = new UsageTracker();
        tracker.Observe(At(0, UsageKind.Suspended));
        var sleep = tracker.Observe(At(3600, UsageKind.Active, "code.exe"));
        Assert.Equal(UsageKind.Suspended, sleep!.Kind);
        Assert.Equal(TimeSpan.FromHours(1), sleep.Duration);
        Assert.Null(sleep.Application);
        tracker.Reset();
        Assert.Null(tracker.Observe(At(7200, UsageKind.Active, "code.exe")));
    }

    [Theory]
    [InlineData(299999, 0, 5, (int)UsageKind.Active)]
    [InlineData(300000, 0, 5, (int)UsageKind.Idle)]
    [InlineData(1000, 4294966295, 5, (int)UsageKind.Active)]
    [InlineData(1000, 2000, 5, (int)UsageKind.Unknown)]
    [InlineData(60000, 0, 1, (int)UsageKind.Idle)]
    public void IdleThresholdHandlesBoundaryWrapAndFutureInput(uint current, uint last, int minutes, int expected)
    {
        Assert.Equal((UsageKind)expected, WindowsUsageSource.ClassifyInput(current, last, minutes));
    }

    private static UsageObservation At(int seconds, UsageKind kind, string? application = null) =>
        new(Start.AddSeconds(seconds), seconds * 1000L, kind, application, GuardPhase.Working, false);
}
