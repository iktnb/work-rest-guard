namespace WorkRestGuard;

public sealed class GuardEngine
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan OfflineRestThreshold = TimeSpan.FromMinutes(15);
    private readonly IClock _clock;
    private readonly IScheduleResolver _scheduleResolver;
    private GuardConfig _config;
    private GuardRuntimeState _state;

    public GuardEngine(
        GuardConfig config,
        GuardRuntimeState state,
        IClock clock,
        IScheduleResolver scheduleResolver)
    {
        _config = config;
        _state = state.SchemaVersion == 1 ? state : new GuardRuntimeState();
        _clock = clock;
        _scheduleResolver = scheduleResolver;
    }

    public event Action<GuardRuntimeState>? StateChanged;

    public event Action<GuardEvent>? EventCreated;

    public GuardRuntimeState State => _state.Clone();

    public GuardViewState View
    {
        get
        {
            var showDay = _state.Phase == GuardPhase.DayResting;
            var showNight = _state.Phase == GuardPhase.NightLocked;
            var deadline = _state.Phase switch
            {
                GuardPhase.Working => _state.WorkDeadlineUtc,
                GuardPhase.DaySnoozed or GuardPhase.DayResting => _state.DayDeadlineUtc,
                GuardPhase.NightGrace => _state.NightGraceDeadlineUtc,
                GuardPhase.NightLocked => _state.NightEndUtc,
                _ => null
            };
            return new GuardViewState(
                _state.Phase,
                showDay || showNight,
                showDay ? OverlayKind.Day : showNight ? OverlayKind.Night : null,
                deadline,
                showDay && !_state.DaySnoozeUsed,
                showNight && !_state.NightGraceUsed,
                _state.NightScheduleId);
        }
    }

    public void Initialize()
    {
        var now = _clock.UtcNow;
        var elapsed = _state.LastHeartbeatUtc == default
            ? TimeSpan.Zero
            : now - _state.LastHeartbeatUtc;
        var activeNight = _scheduleResolver.GetActiveNightWindow(now, _config);
        var changed = false;

        if (activeNight is not null)
        {
            changed = ReconcileActiveNight(activeNight, now, initializing: true);
        }
        else if (_state.Phase is GuardPhase.NightLocked or GuardPhase.NightGrace)
        {
            CreateEvent(GuardEventType.NightInterrupted, verified: false);
            EnterReady(now);
            changed = true;
        }
        else if (elapsed >= OfflineRestThreshold &&
                 _state.Phase != GuardPhase.Ready)
        {
            if (_state.Phase == GuardPhase.DayResting)
            {
                CreateEvent(GuardEventType.DayBreakCompleted, verified: false);
            }

            EnterReady(now);
            changed = true;
        }

        _state.LastHeartbeatUtc = now;
        if (changed || elapsed > HeartbeatInterval)
        {
            RaiseStateChanged();
        }
    }

    public void ApplyConfig(GuardConfig config)
    {
        var now = _clock.UtcNow;
        var wasNightLocked = _state.Phase is GuardPhase.NightLocked or GuardPhase.NightGrace;
        _config = config;

        var activeNight = _scheduleResolver.GetActiveNightWindow(now, _config);
        if (!_config.GuardEnabled ||
            !_config.IsConfigured ||
            (wasNightLocked && activeNight is null))
        {
            if (wasNightLocked)
            {
                CreateEvent(
                    GuardEventType.NightInterrupted,
                    nightScheduleId: _state.NightScheduleId);
            }

            EnterReady(now);
            RaiseStateChanged();
            return;
        }

        Tick(inputChanged: false);
    }

    public void Tick(bool inputChanged)
    {
        if (!_config.GuardEnabled || !_config.IsConfigured)
        {
            return;
        }

        var now = _clock.UtcNow;
        var changed = false;
        var activeNight = _scheduleResolver.GetActiveNightWindow(now, _config);

        if (activeNight is not null)
        {
            changed |= ReconcileActiveNight(activeNight, now, initializing: false);
            if (_state.Phase is GuardPhase.NightLocked or GuardPhase.NightGrace)
            {
                changed |= UpdateHeartbeat(now);
                if (changed)
                {
                    RaiseStateChanged();
                }

                return;
            }
        }
        else
        {
            if (_state.Phase is GuardPhase.NightLocked or GuardPhase.NightGrace)
            {
                CreateEvent(GuardEventType.NightCompleted);
                EnterReady(now);
                changed = true;
            }

            if (_state.NightOverrideScheduleId is not null)
            {
                _state.NightOverrideScheduleId = null;
                _state.NightScheduleId = null;
                _state.NightEndUtc = null;
                changed = true;
            }
        }

        changed |= TickDayCycle(now, inputChanged);
        changed |= UpdateHeartbeat(now);
        if (changed)
        {
            RaiseStateChanged();
        }
    }

    public bool StartManualBreak()
    {
        if (!_config.GuardEnabled || !_config.IsConfigured)
        {
            return false;
        }

        var now = _clock.UtcNow;
        if (_scheduleResolver.GetActiveNightWindow(now, _config) is not null &&
            _state.NightOverrideScheduleId is null)
        {
            return false;
        }

        _state.Phase = GuardPhase.DayResting;
        _state.DayDeadlineUtc = now.AddMinutes(_config.BreakMinutes);
        _state.DaySnoozeUsed = false;
        _state.WorkDeadlineUtc = null;
        _state.LastHeartbeatUtc = now;
        RaiseStateChanged();
        return true;
    }

    public bool SnoozeDay(int minutes)
    {
        if (_state.Phase != GuardPhase.DayResting ||
            _state.DaySnoozeUsed ||
            minutes is not (5 or 15 or 30))
        {
            return false;
        }

        var now = _clock.UtcNow;
        _state.Phase = GuardPhase.DaySnoozed;
        _state.DayDeadlineUtc = now.AddMinutes(minutes);
        _state.DaySnoozeUsed = true;
        _state.LastHeartbeatUtc = now;
        CreateEvent(GuardEventType.DaySnoozed, snoozeMinutes: minutes);
        RaiseStateChanged();
        return true;
    }

    public bool SkipDayBreak()
    {
        if (_state.Phase != GuardPhase.DayResting)
        {
            return false;
        }

        var now = _clock.UtcNow;
        EnterWorking(now);
        CreateEvent(GuardEventType.DayBreakSkipped);
        RaiseStateChanged();
        return true;
    }

    public bool UseNightGrace()
    {
        var now = _clock.UtcNow;
        var activeNight = _scheduleResolver.GetActiveNightWindow(now, _config);
        if (_state.Phase != GuardPhase.NightLocked ||
            _state.NightGraceUsed ||
            activeNight is null ||
            !string.Equals(_state.NightScheduleId, activeNight.Id, StringComparison.Ordinal))
        {
            return false;
        }

        _state.Phase = GuardPhase.NightGrace;
        _state.NightGraceUsed = true;
        _state.NightGraceDeadlineUtc = Min(now.AddMinutes(10), activeNight.EndsAtUtc);
        _state.LastHeartbeatUtc = now;
        CreateEvent(
            GuardEventType.NightGraceUsed,
            snoozeMinutes: 10,
            nightScheduleId: activeNight.Id);
        RaiseStateChanged();
        return true;
    }

    public bool OverrideNight()
    {
        var now = _clock.UtcNow;
        var activeNight = _scheduleResolver.GetActiveNightWindow(now, _config);
        if (activeNight is null ||
            _state.Phase is not (GuardPhase.NightLocked or GuardPhase.NightGrace))
        {
            return false;
        }

        _state.NightScheduleId = activeNight.Id;
        _state.NightEndUtc = activeNight.EndsAtUtc;
        _state.NightOverrideScheduleId = activeNight.Id;
        _state.NightGraceDeadlineUtc = null;
        EnterWorking(now, preserveNight: true);
        CreateEvent(
            GuardEventType.NightOverridden,
            nightScheduleId: activeNight.Id);
        RaiseStateChanged();
        return true;
    }

    public void RecordEmergencyExit()
    {
        CreateEvent(
            GuardEventType.EmergencyExit,
            nightScheduleId: _state.NightScheduleId);
    }

    private bool ReconcileActiveNight(
        NightWindow activeNight,
        DateTimeOffset now,
        bool initializing)
    {
        if (string.Equals(
                _state.NightOverrideScheduleId,
                activeNight.Id,
                StringComparison.Ordinal))
        {
            _state.NightScheduleId = activeNight.Id;
            _state.NightEndUtc = activeNight.EndsAtUtc;
            if (_state.Phase is GuardPhase.NightLocked or GuardPhase.NightGrace)
            {
                EnterWorking(now, preserveNight: true);
                return true;
            }

            return false;
        }

        if (!string.Equals(_state.NightScheduleId, activeNight.Id, StringComparison.Ordinal))
        {
            _state.Phase = GuardPhase.NightLocked;
            _state.NightScheduleId = activeNight.Id;
            _state.NightEndUtc = activeNight.EndsAtUtc;
            _state.NightGraceDeadlineUtc = null;
            _state.NightGraceUsed = false;
            _state.NightOverrideScheduleId = null;
            _state.WorkDeadlineUtc = null;
            _state.DayDeadlineUtc = null;
            CreateEvent(
                GuardEventType.NightLocked,
                nightScheduleId: activeNight.Id,
                verified: !initializing);
            return true;
        }

        _state.NightEndUtc = activeNight.EndsAtUtc;
        if (_state.Phase == GuardPhase.NightGrace &&
            _state.NightGraceDeadlineUtc <= now)
        {
            _state.Phase = GuardPhase.NightLocked;
            _state.NightGraceDeadlineUtc = null;
            return true;
        }

        if (_state.Phase is not (GuardPhase.NightLocked or GuardPhase.NightGrace))
        {
            _state.Phase = GuardPhase.NightLocked;
            _state.WorkDeadlineUtc = null;
            _state.DayDeadlineUtc = null;
            return true;
        }

        return false;
    }

    private bool TickDayCycle(DateTimeOffset now, bool inputChanged)
    {
        switch (_state.Phase)
        {
            case GuardPhase.Ready when inputChanged:
                EnterWorking(now);
                return true;

            case GuardPhase.Working when _state.WorkDeadlineUtc <= now:
                _state.Phase = GuardPhase.DayResting;
                _state.WorkDeadlineUtc = null;
                _state.DayDeadlineUtc = now.AddMinutes(_config.BreakMinutes);
                _state.DaySnoozeUsed = false;
                return true;

            case GuardPhase.DaySnoozed when _state.DayDeadlineUtc <= now:
                _state.Phase = GuardPhase.DayResting;
                _state.DayDeadlineUtc = now.AddMinutes(_config.BreakMinutes);
                return true;

            case GuardPhase.DayResting when _state.DayDeadlineUtc <= now:
                CreateEvent(GuardEventType.DayBreakCompleted);
                EnterReady(now);
                return true;

            default:
                return false;
        }
    }

    private bool UpdateHeartbeat(DateTimeOffset now)
    {
        if (_state.LastHeartbeatUtc != default &&
            now - _state.LastHeartbeatUtc < HeartbeatInterval)
        {
            return false;
        }

        _state.LastHeartbeatUtc = now;
        return true;
    }

    private void EnterReady(DateTimeOffset now)
    {
        _state.Phase = GuardPhase.Ready;
        _state.WorkDeadlineUtc = null;
        _state.DayDeadlineUtc = null;
        _state.DaySnoozeUsed = false;
        _state.NightGraceDeadlineUtc = null;
        _state.LastHeartbeatUtc = now;
    }

    private void EnterWorking(DateTimeOffset now, bool preserveNight = false)
    {
        _state.Phase = GuardPhase.Working;
        _state.WorkDeadlineUtc = now.AddMinutes(_config.WorkMinutes);
        _state.DayDeadlineUtc = null;
        _state.DaySnoozeUsed = false;
        _state.NightGraceDeadlineUtc = null;
        if (!preserveNight)
        {
            _state.NightScheduleId = null;
            _state.NightEndUtc = null;
            _state.NightOverrideScheduleId = null;
        }

        _state.LastHeartbeatUtc = now;
    }

    private void CreateEvent(
        GuardEventType type,
        int? snoozeMinutes = null,
        string? nightScheduleId = null,
        bool verified = true)
    {
        EventCreated?.Invoke(new GuardEvent(
            Guid.NewGuid(),
            _clock.UtcNow,
            type,
            snoozeMinutes,
            nightScheduleId,
            verified));
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(State);

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) =>
        left <= right ? left : right;
}
