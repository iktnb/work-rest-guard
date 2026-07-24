namespace WorkRestGuard;

internal sealed class StatusTimerController : IDisposable
{
    private static readonly TimeSpan ReassertInterval = TimeSpan.FromSeconds(15);
    private StatusTimerForm? _timer;
    private DateTimeOffset _nextReassertUtc;

    internal void Update(
        GuardViewState view,
        MonitorDescriptor monitor,
        string languageCode,
        DateTimeOffset nowUtc)
    {
        if (view.ShowOverlay ||
            view.DeadlineUtc is null ||
            view.Phase is not (
                GuardPhase.Working or
                GuardPhase.DaySnoozed or
                GuardPhase.NightGrace))
        {
            Hide();
            return;
        }

        if (_timer is null || _timer.IsDisposed)
        {
            _timer = new StatusTimerForm(
                monitor,
                view,
                languageCode,
                nowUtc);
            _timer.Show();
        }
        else
        {
            _timer.UpdateView(view, monitor, languageCode, nowUtc);
        }

        _timer.Reassert(monitor);
        _nextReassertUtc = nowUtc.Add(ReassertInterval);
    }

    internal void Hide()
    {
        if (_timer is null)
        {
            return;
        }

        var timer = _timer;
        _timer = null;
        if (!timer.IsDisposed)
        {
            timer.Close();
            timer.Dispose();
        }
    }

    internal void Tick(
        GuardViewState view,
        MonitorDescriptor monitor,
        DateTimeOffset nowUtc)
    {
        if (_timer is null ||
            _timer.IsDisposed ||
            view.ShowOverlay ||
            view.DeadlineUtc is null)
        {
            return;
        }

        _timer.UpdateCountdown(view.DeadlineUtc, nowUtc);
        if (nowUtc >= _nextReassertUtc)
        {
            _timer.Reassert(monitor);
            _nextReassertUtc = nowUtc.Add(ReassertInterval);
        }
    }

    public void Dispose() => Hide();
}
