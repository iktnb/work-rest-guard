namespace WorkRestGuard.Tests;

public sealed class GuardEngineTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 19, 30, 0, TimeSpan.Zero);

    [Fact]
    public void FirstInputStartsWorkAndDeadlineBecomesOnlyARestReminder()
    {
        var clock = new FakeClock(Start);
        var engine = new GuardEngine(TestData.Config(), new(), clock);
        var events = new List<GuardEvent>();
        engine.EventCreated += events.Add;
        engine.Initialize();
        engine.Tick(false);
        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        engine.Tick(true);
        Assert.Equal(Start.AddMinutes(90), engine.View.DeadlineUtc);
        clock.Advance(TimeSpan.FromMinutes(90));
        engine.Tick(true);
        Assert.Equal(GuardPhase.DayResting, engine.State.Phase);
        Assert.Equal(clock.UtcNow.AddMinutes(15), engine.View.DeadlineUtc);
        Assert.False(engine.View.ShowOverlay);
        Assert.False(engine.View.DaySnoozeAvailable);
        Assert.False(engine.View.NightGraceAvailable);
        Assert.Null(engine.View.OverlayKind);
        clock.Advance(TimeSpan.FromMinutes(15));
        engine.Tick(true);
        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Single(events, e => e.Type == GuardEventType.DayBreakCompleted);
        engine.Tick(true);
        Assert.Equal(GuardPhase.Working, engine.State.Phase);
    }

    [Fact]
    public void LegacyNightConfigurationCannotPreemptWork()
    {
        var clock = new FakeClock(Start);
        var engine = new GuardEngine(TestData.Config(), new(), clock);
        engine.Initialize();
        engine.Tick(true);
        var deadline = engine.View.DeadlineUtc;
        clock.Advance(TimeSpan.FromMinutes(30));
        engine.Tick(false);
        Assert.Equal(GuardPhase.Working, engine.State.Phase);
        Assert.Equal(deadline, engine.View.DeadlineUtc);
        Assert.False(engine.View.ShowOverlay);
    }

    [Theory]
    [InlineData(GuardPhase.NightLocked)]
    [InlineData(GuardPhase.NightGrace)]
    [InlineData(GuardPhase.DaySnoozed)]
    public void LegacyStateRecoversToReadyAndClearsNightFields(GuardPhase phase)
    {
        var state = new GuardRuntimeState
        {
            Phase = phase,
            LastHeartbeatUtc = Start,
            NightScheduleId = "old",
            NightEndUtc = Start.AddHours(10),
            NightGraceUsed = true,
            NightOverrideScheduleId = "old",
            DaySnoozeUsed = true
        };
        var engine = new GuardEngine(TestData.Config(), state, new FakeClock(Start));
        engine.Initialize();
        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Null(engine.State.NightScheduleId);
        Assert.Null(engine.State.NightEndUtc);
        Assert.False(engine.State.NightGraceUsed);
        Assert.False(engine.State.DaySnoozeUsed);
        Assert.Null(engine.State.NightOverrideScheduleId);
        Assert.False(engine.View.ShowOverlay);
    }

    [Theory]
    [InlineData(14, GuardPhase.Working)]
    [InlineData(15, GuardPhase.Ready)]
    public void RestartPreservesShortWorkCycleAndResetsAfterLongAbsence(int minutes, GuardPhase expected)
    {
        var clock = new FakeClock(Start);
        var engine = new GuardEngine(TestData.Config(), new(), clock);
        engine.Initialize();
        engine.Tick(true);
        var saved = engine.State;
        clock.Advance(TimeSpan.FromMinutes(minutes));
        var restored = new GuardEngine(TestData.Config(), saved, clock);
        restored.Initialize();
        Assert.Equal(expected, restored.State.Phase);
        Assert.Equal(GuardPhase.Working, saved.Phase);
    }

    [Theory]
    [InlineData(GuardPhase.Working)]
    [InlineData(GuardPhase.DayResting)]
    public void MissingDeadlineRecoversToReady(GuardPhase phase)
    {
        var engine = new GuardEngine(TestData.Config(), new() { Phase = phase, LastHeartbeatUtc = Start }, new FakeClock(Start));
        engine.Initialize();
        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
    }

    [Fact]
    public void ManualRestAndDisablingRemindersNeverRequestABlocker()
    {
        var config = TestData.Config();
        var engine = new GuardEngine(config, new(), new FakeClock(Start));
        engine.Initialize();
        Assert.True(engine.StartManualBreak());
        Assert.Equal(GuardPhase.DayResting, engine.State.Phase);
        Assert.False(engine.View.ShowOverlay);
        config.GuardEnabled = false;
        engine.ApplyConfig(config);
        Assert.False(engine.StartManualBreak());
        engine.Tick(true);
        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Null(engine.View.DeadlineUtc);
    }

    [Fact]
    public void ClockRollbackResetsAndPersistsTheTimer()
    {
        var clock = new FakeClock(Start);
        var engine = new GuardEngine(TestData.Config(), new(), clock);
        engine.Initialize();
        engine.Tick(true);
        var changes = 0;
        engine.StateChanged += _ => changes++;
        clock.Advance(TimeSpan.FromMinutes(-5));
        engine.Tick(false);
        Assert.Equal(GuardPhase.Ready, engine.State.Phase);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void HeartbeatDoesNotChangeTheRenderedView()
    {
        var clock = new FakeClock(Start);
        var engine = new GuardEngine(TestData.Config(), new(), clock);
        engine.Initialize();
        engine.Tick(true);
        var view = engine.View;
        clock.Advance(TimeSpan.FromSeconds(30));
        engine.Tick(false);
        Assert.Equal(view, engine.View);
        Assert.Equal(clock.UtcNow, engine.State.LastHeartbeatUtc);
    }
}
