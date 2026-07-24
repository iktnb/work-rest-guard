namespace WorkRestGuard.Tests;

public sealed class GuardEngineTests
{
    [Fact]
    public void FirstInputStartsNinetyMinuteWorkCycle()
    {
        var start = Utc(2026, 7, 20, 8, 0);
        var (engine, clock, _) = CreateEngine(start);
        engine.Initialize();

        engine.Tick(inputChanged: true);

        Assert.Equal(GuardPhase.Working, engine.State.Phase);
        Assert.Equal(start.AddMinutes(90), engine.State.WorkDeadlineUtc);
        Assert.Equal(start.AddMinutes(90), engine.View.DeadlineUtc);
        Assert.False(engine.View.ShowOverlay);
        Assert.Equal(start, clock.UtcNow);
    }

    [Fact]
    public void HeartbeatPersistenceDoesNotChangeTheRenderedView()
    {
        var (engine, clock, _) = CreateEngine(Utc(2026, 7, 20, 8, 0));
        engine.Initialize();
        engine.Tick(inputChanged: true);
        var beforeHeartbeat = engine.View;

        clock.Advance(TimeSpan.FromSeconds(30));
        engine.Tick(inputChanged: false);

        Assert.Equal(beforeHeartbeat, engine.View);
    }

    [Fact]
    public void DaySnoozeCanBeUsedOnlyOnceAndReturnsToFreshBreak()
    {
        var (engine, clock, events) = CreateEngine(Utc(2026, 7, 20, 8, 0));
        engine.Initialize();
        engine.Tick(inputChanged: true);
        clock.Advance(TimeSpan.FromMinutes(90));
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.DayResting, engine.State.Phase);
        Assert.True(engine.View.DaySnoozeAvailable);
        Assert.True(engine.SnoozeDay(15));
        Assert.False(engine.SnoozeDay(5));
        Assert.Equal(GuardPhase.DaySnoozed, engine.State.Phase);

        clock.Advance(TimeSpan.FromMinutes(15));
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.DayResting, engine.State.Phase);
        Assert.Equal(clock.UtcNow.AddMinutes(15), engine.State.DayDeadlineUtc);
        Assert.False(engine.View.DaySnoozeAvailable);

        clock.Advance(TimeSpan.FromMinutes(15));
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Contains(events, item => item.Type == GuardEventType.DayBreakCompleted);
        Assert.Single(events, item => item.Type == GuardEventType.DaySnoozed);
    }

    [Fact]
    public void ExactDayOverrideStartsNewWorkCycle()
    {
        var (engine, clock, events) = CreateEngine(Utc(2026, 7, 20, 8, 0));
        engine.Initialize();
        Assert.True(engine.StartManualBreak());

        Assert.True(engine.SkipDayBreak());

        Assert.Equal(GuardPhase.Working, engine.State.Phase);
        Assert.Equal(clock.UtcNow.AddMinutes(90), engine.State.WorkDeadlineUtc);
        Assert.Contains(events, item => item.Type == GuardEventType.DayBreakSkipped);
    }

    [Fact]
    public void NightModePreemptsActiveDayCycle()
    {
        var (engine, clock, _) = CreateEngine(Utc(2026, 7, 20, 19, 30));
        engine.Initialize();
        engine.Tick(inputChanged: true);

        clock.UtcNow = Utc(2026, 7, 20, 20, 0);
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.NightLocked, engine.State.Phase);
        Assert.Null(engine.State.WorkDeadlineUtc);
        Assert.Equal(OverlayKind.Night, engine.View.OverlayKind);
        Assert.True(engine.View.NightGraceAvailable);
    }

    [Fact]
    public void NightGraceIsTenMinutesAndCanBeUsedOnlyOnce()
    {
        var (engine, clock, events) = CreateEngine(Utc(2026, 7, 20, 21, 0));
        engine.Initialize();

        Assert.True(engine.UseNightGrace());
        Assert.False(engine.UseNightGrace());
        Assert.Equal(GuardPhase.NightGrace, engine.State.Phase);
        Assert.Equal(clock.UtcNow.AddMinutes(10), engine.State.NightGraceDeadlineUtc);

        clock.Advance(TimeSpan.FromMinutes(10));
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.NightLocked, engine.State.Phase);
        Assert.False(engine.View.NightGraceAvailable);
        Assert.Single(events, item => item.Type == GuardEventType.NightGraceUsed);
    }

    [Fact]
    public void NightOverridePersistsForWindowAndStartsNewDayCycle()
    {
        var (engine, clock, events) = CreateEngine(Utc(2026, 7, 20, 21, 0));
        engine.Initialize();

        Assert.True(engine.OverrideNight());

        Assert.Equal(GuardPhase.Working, engine.State.Phase);
        Assert.Equal("2026-07-20", engine.State.NightOverrideScheduleId);
        Assert.Equal(clock.UtcNow.AddMinutes(90), engine.State.WorkDeadlineUtc);

        clock.Advance(TimeSpan.FromMinutes(30));
        engine.Tick(inputChanged: false);
        Assert.Equal(GuardPhase.Working, engine.State.Phase);
        Assert.Contains(events, item => item.Type == GuardEventType.NightOverridden);
    }

    [Fact]
    public void NaturalNightCompletionReturnsToReadyAtSix()
    {
        var (engine, clock, events) = CreateEngine(Utc(2026, 7, 20, 23, 0));
        engine.Initialize();

        clock.UtcNow = Utc(2026, 7, 21, 6, 0);
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Contains(events, item => item.Type == GuardEventType.NightCompleted);
    }

    [Fact]
    public void DayCycleContinuesAcrossSixAfterNightOverride()
    {
        var (engine, clock, _) = CreateEngine(Utc(2026, 7, 21, 5, 30));
        engine.Initialize();
        Assert.True(engine.OverrideNight());
        var deadline = engine.State.WorkDeadlineUtc;

        clock.UtcNow = Utc(2026, 7, 21, 6, 0);
        engine.Tick(inputChanged: false);

        Assert.Equal(GuardPhase.Working, engine.State.Phase);
        Assert.Equal(deadline, engine.State.WorkDeadlineUtc);
        Assert.Null(engine.State.NightOverrideScheduleId);
    }

    [Fact]
    public void RestartDuringNightRestoresLockAndGraceUsage()
    {
        var start = Utc(2026, 7, 20, 21, 0);
        var (first, clock, _) = CreateEngine(start);
        first.Initialize();
        Assert.True(first.UseNightGrace());
        var persisted = first.State;

        clock.Advance(TimeSpan.FromMinutes(10));
        var restored = new GuardEngine(
            TestData.Config(),
            persisted,
            clock,
            new ScheduleResolver(TimeZoneInfo.Utc));
        restored.Initialize();

        Assert.Equal(GuardPhase.NightLocked, restored.State.Phase);
        Assert.True(restored.State.NightGraceUsed);
        Assert.False(restored.View.NightGraceAvailable);
    }

    [Fact]
    public void FifteenMinutesOfflineCompletesDayRest()
    {
        var start = Utc(2026, 7, 20, 8, 0);
        var (first, clock, _) = CreateEngine(start);
        first.Initialize();
        Assert.True(first.StartManualBreak());
        var persisted = first.State;

        clock.Advance(TimeSpan.FromMinutes(15));
        var restored = new GuardEngine(
            TestData.Config(),
            persisted,
            clock,
            new ScheduleResolver(TimeZoneInfo.Utc));
        restored.Initialize();

        Assert.Equal(GuardPhase.Ready, restored.State.Phase);
    }

    [Fact]
    public void OfflineRestThresholdIsAlwaysFifteenMinutes()
    {
        var start = Utc(2026, 7, 20, 8, 0);
        var longBreakConfig = TestData.Config();
        longBreakConfig.BreakMinutes = 60;
        var clock = new FakeClock(start);
        var first = new GuardEngine(
            longBreakConfig,
            new GuardRuntimeState(),
            clock,
            new ScheduleResolver(TimeZoneInfo.Utc));
        first.Initialize();
        first.Tick(inputChanged: true);
        var persisted = first.State;

        clock.Advance(TimeSpan.FromMinutes(14));
        var tooSoon = new GuardEngine(
            longBreakConfig,
            persisted.Clone(),
            clock,
            new ScheduleResolver(TimeZoneInfo.Utc));
        tooSoon.Initialize();
        Assert.Equal(GuardPhase.Working, tooSoon.State.Phase);

        clock.Advance(TimeSpan.FromMinutes(1));
        var rested = new GuardEngine(
            longBreakConfig,
            persisted.Clone(),
            clock,
            new ScheduleResolver(TimeZoneInfo.Utc));
        rested.Initialize();
        Assert.Equal(GuardPhase.Ready, rested.State.Phase);
    }

    [Fact]
    public void DisablingNightProtectionRecordsAnInterruption()
    {
        var (engine, _, events) = CreateEngine(Utc(2026, 7, 20, 21, 0));
        engine.Initialize();
        var disabled = TestData.Config();
        disabled.NightModeEnabled = false;

        engine.ApplyConfig(disabled);

        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Contains(
            events,
            item =>
                item.Type == GuardEventType.NightInterrupted &&
                item.NightScheduleId == "2026-07-20");
        Assert.DoesNotContain(events, item => item.Type == GuardEventType.NightCompleted);
    }

    private static (
        GuardEngine Engine,
        FakeClock Clock,
        List<GuardEvent> Events) CreateEngine(DateTimeOffset now)
    {
        var clock = new FakeClock(now);
        var engine = new GuardEngine(
            TestData.Config(),
            new GuardRuntimeState(),
            clock,
            new ScheduleResolver(TimeZoneInfo.Utc));
        var events = new List<GuardEvent>();
        engine.EventCreated += events.Add;
        return (engine, clock, events);
    }

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);
}
