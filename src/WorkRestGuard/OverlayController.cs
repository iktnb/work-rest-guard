namespace WorkRestGuard;

internal sealed class OverlayController : IDisposable
{
    private readonly Action<int> _daySnooze;
    private readonly Action _daySkip;
    private readonly Action _nightGrace;
    private readonly Action _nightOverride;
    private GuardOverlayForm? _overlay;

    internal OverlayController(
        Action<int> daySnooze,
        Action daySkip,
        Action nightGrace,
        Action nightOverride)
    {
        _daySnooze = daySnooze;
        _daySkip = daySkip;
        _nightGrace = nightGrace;
        _nightOverride = nightOverride;
    }

    internal void Update(
        GuardViewState view,
        MonitorDescriptor monitor,
        string languageCode,
        DateTimeOffset nowUtc)
    {
        if (!view.ShowOverlay || view.OverlayKind is null)
        {
            Hide();
            return;
        }

        if (_overlay is null ||
            _overlay.IsDisposed ||
            _overlay.Kind != view.OverlayKind ||
            !string.Equals(
                _overlay.LanguageCode,
                languageCode,
                StringComparison.OrdinalIgnoreCase))
        {
            Hide();
            _overlay = new GuardOverlayForm(
                monitor,
                view,
                languageCode,
                _daySnooze,
                _daySkip,
                _nightGrace,
                _nightOverride);
            _overlay.Show();
        }
        else
        {
            _overlay.UpdateView(view, monitor, languageCode, nowUtc);
        }
    }

    internal void Tick(
        GuardViewState view,
        MonitorDescriptor monitor,
        DateTimeOffset nowUtc)
    {
        if (_overlay is null || _overlay.IsDisposed || !view.ShowOverlay)
        {
            return;
        }

        _overlay.UpdateCountdown(nowUtc);
        _overlay.Reassert(monitor);
    }

    internal void SetEmergencyProgress(TimeSpan? remaining) =>
        _overlay?.SetEmergencyProgress(remaining);

    internal void Hide()
    {
        if (_overlay is null)
        {
            return;
        }

        var overlay = _overlay;
        _overlay = null;
        if (!overlay.IsDisposed)
        {
            overlay.AllowClose();
            overlay.Dispose();
        }
    }

    public void Dispose() => Hide();
}
