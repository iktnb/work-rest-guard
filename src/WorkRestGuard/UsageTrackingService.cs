namespace WorkRestGuard;

internal sealed class UsageTrackingService
{
    private readonly UsageTracker _tracker = new();
    private readonly IUsageSource _source;
    private readonly IClock _clock;
    private readonly Func<long> _ticks;
    private readonly FileLogger _logger;
    private GuardConfig _config;
    private bool _enabled;
    private bool _locked;
    private bool _suspended;
    private bool _observationError;
    private bool _flushError;
    private long? _lastSample;
    private long? _lastFlush;

    internal UsageTrackingService(
        UsageStore store, FileLogger logger, GuardConfig config, IClock clock,
        bool safeMode = false, IUsageSource? source = null, Func<long>? ticks = null)
    {
        Store = store;
        _logger = logger;
        _config = config.Clone();
        _clock = clock;
        _source = source ?? new WindowsUsageSource();
        _ticks = ticks ?? (() => Environment.TickCount64);
        SafeMode = safeMode;
        _enabled = !safeMode;
    }

    internal UsageStore Store { get; }
    internal bool SafeMode { get; }
    internal bool Enabled => _enabled;
    internal bool HasError => _observationError || _flushError;

    internal void ApplyConfig(GuardConfig config, GuardPhase phase)
    {
        Capture(phase, force: true);
        Flush();
        _tracker.Reset();
        _lastSample = null;
        _config = config.Clone();
        _enabled = !SafeMode;
        Capture(phase, force: true);
    }

    internal void SetLocked(bool locked, GuardPhase phase)
    {
        Capture(phase, force: true);
        _locked = locked;
        Capture(phase, force: true);
        Flush();
    }

    internal void SetSuspended(bool suspended, GuardPhase phase)
    {
        Capture(phase, force: true);
        _suspended = suspended;
        Capture(phase, force: true);
        Flush();
    }

    internal void Capture(GuardPhase phase, bool force = false)
    {
        if (!_enabled)
        {
            return;
        }

        var ticks = _ticks();
        if (!force && _lastSample is not null && ticks - _lastSample < 5000)
        {
            return;
        }

        _lastSample = ticks;
        try
        {
            var snapshot = _suspended ? new UsageSnapshot(UsageKind.Suspended, null)
                : _locked ? new UsageSnapshot(UsageKind.Locked, null)
                : _source.Capture(_config.UsageIdleMinutes);
            var now = _clock.UtcNow;
            var night = TimerPolicy.IsQuietTime(now, _config);
            var span = _tracker.Observe(new UsageObservation(
                now, ticks, snapshot.Kind, snapshot.Application, phase, night));
            if (span is not null)
            {
                Store.Append(span);
                _observationError = false;
            }

            if (_lastFlush is null || ticks - _lastFlush >= 30000)
            {
                Flush();
                _lastFlush = ticks;
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _tracker.Reset();
            ReportError(exception, observation: true);
        }
    }

    internal void Flush()
    {
        try
        {
            Store.Flush(_clock.UtcNow);
            _flushError = false;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            ReportError(exception);
        }
    }

    internal void Stop(GuardPhase phase)
    {
        Capture(phase, force: true);
        _enabled = false;
        _tracker.Reset();
        Flush();
    }

    private void ReportError(Exception exception, bool observation = false)
    {
        if (!HasError)
        {
            try
            {
                _logger.Error("Usage statistics could not be saved; protection is unaffected", exception);
            }
            catch (Exception loggingException) when (loggingException is IOException or UnauthorizedAccessException)
            {
                // A second disk failure must not interrupt the guard engine.
            }
        }

        if (observation)
        {
            _observationError = true;
        }
        else
        {
            _flushError = true;
        }
    }
}
