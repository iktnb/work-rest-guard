namespace WorkRestGuard.Tests;

public sealed class UsageReportTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AppSharesUseOnlyActivityIncludingUnattributedActivityAndGapsRemainVisible()
    {
        var report = UsageReport.Build(
        [
            Span(10, 20, UsageKind.Active, "chrome.exe"),
            Span(20, 30, UsageKind.Active, "code.exe") with { IsNightWindow = true },
            Span(30, 40, UsageKind.Idle),
            Span(50, 55, UsageKind.Active)
        ], Start, Start.AddHours(1), TimeZoneInfo.Utc);
        Assert.Equal(TimeSpan.FromMinutes(25), report.Total(UsageKind.Active));
        Assert.Equal(TimeSpan.FromMinutes(10), report.Total(UsageKind.Idle));
        Assert.Equal(TimeSpan.FromMinutes(25), report.Total(UsageKind.Unrecorded));
        Assert.Equal(1, report.Applications.Sum(app => app.Share), precision: 10);
        Assert.Equal(.2, report.Applications.Single(app => app.Application is null).Share);
        var day = Assert.Single(report.Days);
        Assert.Equal(TimeSpan.FromMinutes(20), day.LongestActive);
        Assert.Equal(TimeSpan.FromMinutes(10), day.NightActive);
        Assert.Equal(Start.AddMinutes(10), day.FirstActive);
        Assert.Equal(Start.AddMinutes(55), day.LastActive);
        Assert.Equal(TimeSpan.FromMinutes(35), day.Recorded);
    }

    [Fact]
    public void RangeClippingAndCaseInsensitiveGroupingDoNotInflateTime()
    {
        var report = UsageReport.Build(
        [Span(0, 20, UsageKind.Active, "Code.exe"), Span(20, 40, UsageKind.Active, "code.exe")],
            Start.AddMinutes(10), Start.AddMinutes(30), TimeZoneInfo.Utc);
        Assert.Equal(TimeSpan.FromMinutes(20), Assert.Single(report.Applications).Duration);
        Assert.Equal(20 * 60, report.Timeline.Sum(span => span.Duration.TotalSeconds));
    }

    [Theory]
    [InlineData(2026, 3, 29, 23)]
    [InlineData(2026, 10, 25, 25)]
    public void LocalDaysRespectKyivDaylightSavingBoundaries(int year, int month, int day, int hours)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
        var date = new DateOnly(year, month, day);
        var start = UsageReport.LocalDayStart(date, zone);
        var end = UsageReport.LocalDayStart(date.AddDays(1), zone);
        var report = UsageReport.Build([], start, end, zone);
        Assert.Equal(TimeSpan.FromHours(hours), report.Total(UsageKind.Unrecorded));
        Assert.Equal(date, Assert.Single(report.Days).Date);
        Assert.Equal(TimeSpan.Zero, report.Days[0].Recorded);
        Assert.Null(report.Days[0].FirstActive);
    }

    [Fact]
    public void ActivityCrossingLocalMidnightIsSplitIntoTwoDays()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("UTC+3", TimeSpan.FromHours(3), "UTC+3", "UTC+3");
        var start = Start.AddHours(20.5);
        var span = new UsageSpan(start, start.AddHours(1), UsageKind.Active, "code.exe", GuardPhase.DayResting, false);
        var report = UsageReport.Build([span], start, start.AddHours(1), zone);
        Assert.Equal(2, report.Days.Count);
        Assert.All(report.Days, day => Assert.Equal(TimeSpan.FromMinutes(30), day.Active));
        Assert.Equal(TimeSpan.FromHours(1), report.Total(UsageKind.Active));
    }

    [Fact]
    public void UnknownLockedAndSuspendedTimeIsNeverApplicationUsage()
    {
        var report = UsageReport.Build(
            [Span(0, 10, UsageKind.Unknown), Span(10, 20, UsageKind.Locked), Span(20, 30, UsageKind.Suspended)],
            Start, Start.AddMinutes(30), TimeZoneInfo.Utc);
        Assert.Empty(report.Applications);
        Assert.Equal(TimeSpan.Zero, report.Total(UsageKind.Active));
        Assert.Equal(TimeSpan.FromMinutes(30), Assert.Single(report.Days).Recorded);
    }

    private static UsageSpan Span(int from, int to, UsageKind kind, string? app = null) =>
        new(Start.AddMinutes(from), Start.AddMinutes(to), kind, app, GuardPhase.Working, false);
}
