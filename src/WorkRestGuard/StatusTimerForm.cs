namespace WorkRestGuard;

internal sealed class StatusTimerForm : Form
{
    private readonly Panel _accent;
    private readonly Label _phase;
    private readonly Label _countdown;
    private readonly bool _targetableForTesting;

    internal StatusTimerForm(
        MonitorDescriptor monitor,
        GuardViewState view,
        string languageCode,
        DateTimeOffset nowUtc,
        bool targetableForTesting = false)
    {
        _targetableForTesting = targetableForTesting;

        Text = targetableForTesting
            ? $"{AppIdentity.ProductName} — Timer Preview"
            : AppIdentity.ProductName;
        Name = nameof(StatusTimerForm);
        AccessibleName = UiText.Get(languageCode, TextKey.TimerAccessibleName);
        AccessibleRole = AccessibleRole.Window;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(230, 78);
        TopMost = true;
        ShowInTaskbar = targetableForTesting;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;

        _accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = Theme.DayAccent
        };
        _phase = new Label
        {
            Dock = DockStyle.Top,
            Height = 27,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI Semibold", 9.5F),
            TextAlign = ContentAlignment.BottomLeft,
            AccessibleName = UiText.Get(languageCode, TextKey.TimerPhaseAccessibleName)
        };
        _countdown = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Text,
            Font = new Font("Consolas", 23F, FontStyle.Bold),
            TextAlign = ContentAlignment.TopLeft,
            AccessibleName = UiText.Get(languageCode, TextKey.TimerAccessibleName)
        };

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 5, 10, 5),
            BackColor = Theme.Surface
        };
        content.Controls.Add(_countdown);
        content.Controls.Add(_phase);
        Controls.Add(content);
        Controls.Add(_accent);

        UpdateView(view, monitor, languageCode, nowUtc);
    }

    protected override bool ShowWithoutActivation => !_targetableForTesting;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExAppWindow = 0x00040000;
            const int wsExNoActivate = 0x08000000;
            var parameters = base.CreateParams;
            if (_targetableForTesting)
            {
                return parameters;
            }

            parameters.ExStyle |= wsExToolWindow | wsExNoActivate;
            parameters.ExStyle &= ~wsExAppWindow;
            return parameters;
        }
    }

    internal void UpdateView(
        GuardViewState view,
        MonitorDescriptor monitor,
        string languageCode,
        DateTimeOffset nowUtc)
    {
        var (labelKey, accent) = view.Phase switch
        {
            GuardPhase.DayResting => (TextKey.TimerRest, Theme.DayAccent),
            _ => (TextKey.TimerWorking, Theme.Success)
        };

        _phase.Text = UiText.Get(languageCode, labelKey);
        _phase.AccessibleDescription = _phase.Text;
        _accent.BackColor = accent;
        UpdateCountdown(view.DeadlineUtc, nowUtc);
        PositionOnMonitor(monitor);
    }

    internal void Reassert(MonitorDescriptor monitor)
    {
        PositionOnMonitor(monitor);
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            Left,
            Top,
            Width,
            Height,
            NativeMethods.SwpNoActivate |
            NativeMethods.SwpNoOwnerZOrder |
            NativeMethods.SwpAsyncWindowPos |
            NativeMethods.SwpShowWindow);
    }

    internal void UpdateCountdown(
        DateTimeOffset? deadlineUtc,
        DateTimeOffset nowUtc)
    {
        var text = CountdownFormatter.FormatRemaining(deadlineUtc, nowUtc);
        if (string.Equals(_countdown.Text, text, StringComparison.Ordinal))
        {
            return;
        }

        _countdown.Text = text;
        _countdown.AccessibleDescription = _countdown.Text;
        AccessibleDescription = $"{_phase.Text}: {_countdown.Text}";
    }

    private void PositionOnMonitor(MonitorDescriptor monitor)
    {
        const int margin = 18;
        var area = monitor.WorkingArea;
        Location = new Point(
            area.Right - Width - margin,
            area.Top + margin);
    }
}
