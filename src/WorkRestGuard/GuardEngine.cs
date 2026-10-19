namespace WorkRestGuard;

public sealed class GuardEngine
{
    private readonly IClock _clock;
    private GuardConfig _config;
    private GuardRuntimeState _state;

    public GuardEngine(GuardConfig config, GuardRuntimeState state, IClock clock)
    {
        _config = config;
        _state = state.SchemaVersion == 1 ? state.Clone() : new GuardRuntimeState();
        _clock = clock;
    }

    public event Action<GuardRuntimeState>? StateChanged;
    public event Action<GuardEvent>? EventCreated;
    public GuardRuntimeState State => _state.Clone();

    // Legacy view fields remain readable, but this version never requests a blocker.
    public GuardViewState View => new(_state.Phase, false, null,
        _state.Phase == GuardPhase.Working ? _state.WorkDeadlineUtc :
        _state.Phase == GuardPhase.DayResting ? _state.DayDeadlineUtc : null,
        false, false, null);

    public void Initialize()
    {
        var now = _clock.UtcNow;
        var elapsed = now - _state.LastHeartbeatUtc;
        if (!_config.GuardEnabled || _state.Phase is not (GuardPhase.Ready or GuardPhase.Working or GuardPhase.DayResting) ||
            elapsed >= TimeSpan.FromMinutes(15) || elapsed < TimeSpan.FromSeconds(-2) ||
            (_state.Phase == GuardPhase.Working && _state.WorkDeadlineUtc is null) ||
            (_state.Phase == GuardPhase.DayResting && _state.DayDeadlineUtc is null))
        {
            EnterReady(now);
        }

        _state.NightScheduleId = null;
        _state.NightEndUtc = null;
        _state.NightGraceDeadlineUtc = null;
        _state.NightOverrideScheduleId = null;
        _state.NightGraceUsed = false;
        _state.DaySnoozeUsed = false;
        _state.LastHeartbeatUtc = now;
        Tick(inputChanged: false);
        RaiseStateChanged();
    }

    public void ApplyConfig(GuardConfig config)
    {
        _config = config;
        if (!config.GuardEnabled)
        {
            EnterReady(_clock.UtcNow);
        }

        RaiseStateChanged();
    }

    public void Tick(bool inputChanged)
    {
        var now = _clock.UtcNow;
        var changed = false;
        if (now < _state.LastHeartbeatUtc.AddSeconds(-2))
        {
            EnterReady(now);
            changed = true;
        }
        if (_config.GuardEnabled)
        {
            switch (_state.Phase)
            {
                case GuardPhase.Ready when inputChanged:
                    _state.Phase = GuardPhase.Working;
                    _state.WorkDeadlineUtc = now.AddMinutes(_config.WorkMinutes);
                    changed = true;
                    break;
                case GuardPhase.Working when _state.WorkDeadlineUtc <= now:
                    EnterRestReminder(now);
                    changed = true;
                    break;
                case GuardPhase.DayResting when _state.DayDeadlineUtc <= now:
                    // The reminder timer ended; this is not proof of actual rest.
                    EventCreated?.Invoke(new GuardEvent(Guid.NewGuid(), now, GuardEventType.DayBreakCompleted));
                    EnterReady(now);
                    changed = true;
                    break;
            }
        }

        if (now - _state.LastHeartbeatUtc >= TimeSpan.FromSeconds(30))
        {
            _state.LastHeartbeatUtc = now;
            changed = true;
        }

        if (changed)
        {
            RaiseStateChanged();
        }
    }

    public bool StartManualBreak()
    {
        if (!_config.GuardEnabled)
        {
            return false;
        }

        EnterRestReminder(_clock.UtcNow);
        RaiseStateChanged();
        return true;
    }

    private void EnterRestReminder(DateTimeOffset now)
    {
        _state.Phase = GuardPhase.DayResting;
        _state.DayDeadlineUtc = now.AddMinutes(_config.BreakMinutes);
        _state.WorkDeadlineUtc = null;
        _state.LastHeartbeatUtc = now;
    }

    private void EnterReady(DateTimeOffset now)
    {
        _state.Phase = GuardPhase.Ready;
        _state.WorkDeadlineUtc = null;
        _state.DayDeadlineUtc = null;
        _state.LastHeartbeatUtc = now;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(State);
}
