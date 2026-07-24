namespace WorkRestGuard.Tests;

public sealed class HistoryStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "work-rest-history-" + Guid.NewGuid());
    private readonly string _path;
    private readonly FileLogger _logger;
    private readonly HistoryStore _store;

    public HistoryStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "history.json");
        _logger = new FileLogger(Path.Combine(_directory, "test.log"));
        _store = new HistoryStore(_logger, _path, TimeZoneInfo.Utc);
    }

    [Fact]
    public void DailySummarySeparatesOutcomes()
    {
        var now = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
        Append(now, GuardEventType.DayBreakCompleted);
        Append(now.AddMinutes(1), GuardEventType.DaySnoozed, 15);
        Append(now.AddMinutes(2), GuardEventType.NightOverridden);

        var today = _store.GetDailySummaries(now).First();

        Assert.Equal(1, today.DayCompleted);
        Assert.Equal(1, today.DaySnoozed);
        Assert.Equal(1, today.NightOverridden);
        Assert.Equal(0, today.NightCompleted);
    }

    [Fact]
    public void EventsOlderThanThirtyCalendarDaysArePruned()
    {
        var now = new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
        Append(now.AddDays(-30), GuardEventType.DayBreakSkipped);
        Append(now.AddDays(-29), GuardEventType.DayBreakCompleted);

        var events = _store.GetEvents(now);

        Assert.Single(events);
        Assert.Equal(GuardEventType.DayBreakCompleted, events[0].Type);
    }

    public void Dispose()
    {
        _logger.Dispose();
        Directory.Delete(_directory, true);
    }

    private void Append(
        DateTimeOffset occurredAt,
        GuardEventType type,
        int? snoozeMinutes = null)
    {
        _store.Append(
            new GuardEvent(
                Guid.NewGuid(),
                occurredAt,
                type,
                snoozeMinutes),
            occurredAt);
    }
}
