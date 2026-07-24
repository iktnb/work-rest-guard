namespace WorkRestGuard;

internal sealed class GuardOverlayForm : Form
{
    private readonly Action<int> _daySnooze;
    private readonly Action _daySkip;
    private readonly Action _nightGrace;
    private readonly Action _nightOverride;
    private readonly Panel _scrollHost;
    private readonly TableLayoutPanel _content;
    private readonly Label _eyebrow;
    private readonly Label _title;
    private readonly Label _detail;
    private readonly Label _countdown;
    private readonly FlowLayoutPanel _graceActions;
    private readonly Button _snooze5;
    private readonly Button _snooze15;
    private readonly Button _snooze30;
    private readonly Button _nightGraceButton;
    private readonly Button _challengeReveal;
    private readonly Label _emergency;
    private ExactTypingPanel? _challenge;
    private GuardViewState _view;
    private string _languageCode;
    private bool _allowClose;
    private readonly bool _targetableForTesting;

    internal GuardOverlayForm(
        MonitorDescriptor monitor,
        GuardViewState view,
        string languageCode,
        Action<int> daySnooze,
        Action daySkip,
        Action nightGrace,
        Action nightOverride,
        bool targetableForTesting = false)
    {
        _view = view;
        _languageCode = languageCode;
        _daySnooze = daySnooze;
        _daySkip = daySkip;
        _nightGrace = nightGrace;
        _nightOverride = nightOverride;
        _targetableForTesting = targetableForTesting;

        Text = targetableForTesting
            ? $"{AppIdentity.ProductName} — Overlay Preview"
            : AppIdentity.ProductName;
        Name = nameof(GuardOverlayForm);
        AccessibleName = AppIdentity.ProductName;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = monitor.Bounds;
        TopMost = true;
        ShowInTaskbar = targetableForTesting;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
        DoubleBuffered = true;

        _eyebrow = new Label
        {
            Text = "WORK REST GUARD",
            ForeColor = Theme.DayAccent,
            Font = new Font("Segoe UI Semibold", 10F),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        _title = new Label
        {
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI Semibold", 27F),
            AutoSize = true,
            MaximumSize = new Size(920, 0),
            Margin = new Padding(0, 0, 0, 12)
        };
        _detail = new Label
        {
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 12F),
            AutoSize = true,
            MaximumSize = new Size(820, 0),
            Margin = new Padding(0, 0, 0, 24)
        };
        _countdown = new Label
        {
            ForeColor = Theme.Text,
            Font = new Font("Consolas", 42F, FontStyle.Bold),
            AutoSize = true,
            AccessibleName = "Countdown",
            Margin = new Padding(0, 0, 0, 26)
        };

        _snooze5 = CreateActionButton(TextKey.Snooze5, 5);
        _snooze15 = CreateActionButton(TextKey.Snooze15, 15);
        _snooze30 = CreateActionButton(TextKey.Snooze30, 30);
        _nightGraceButton = Theme.StyleButton(
            new Button { AccessibleName = UiText.Get(languageCode, TextKey.NightGrace10) },
            Theme.NightAccent,
            filled: true);
        _nightGraceButton.Click += (_, _) => _nightGrace();

        _graceActions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            MinimumSize = new Size(0, 50),
            Margin = new Padding(0, 0, 0, 20)
        };

        _challengeReveal = Theme.StyleButton(
            new Button(),
            Theme.DayAccent,
            filled: false);
        _challengeReveal.MinimumSize = new Size(280, 46);
        _challengeReveal.Click += (_, _) => ShowChallenge();

        _emergency = new Label
        {
            ForeColor = Theme.Danger,
            Font = new Font("Segoe UI Semibold", 10F),
            AutoSize = true,
            Visible = false,
            Margin = new Padding(0, 22, 0, 0),
            AccessibleName = UiText.Get(languageCode, TextKey.EmergencyExit)
        };

        _content = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _content.Controls.Add(_eyebrow);
        _content.Controls.Add(_title);
        _content.Controls.Add(_detail);
        _content.Controls.Add(_countdown);
        _content.Controls.Add(_graceActions);
        _content.Controls.Add(_challengeReveal);
        _content.Controls.Add(_emergency);

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        _scrollHost.Controls.Add(_content);
        Controls.Add(_scrollHost);

        Resize += (_, _) => PositionContent();
        Shown += (_, _) =>
        {
            PositionContent();
            BringToFront();
            Activate();
            _challengeReveal.Focus();
        };

        UpdateView(view, monitor, languageCode, DateTimeOffset.UtcNow);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExAppWindow = 0x00040000;
            var parameters = base.CreateParams;
            if (_targetableForTesting)
            {
                return parameters;
            }

            parameters.ExStyle |= wsExToolWindow;
            parameters.ExStyle &= ~wsExAppWindow;
            return parameters;
        }
    }

    internal OverlayKind Kind => _view.OverlayKind ?? OverlayKind.Day;

    internal string LanguageCode => _languageCode;

    internal void UpdateView(
        GuardViewState view,
        MonitorDescriptor monitor,
        string languageCode,
        DateTimeOffset nowUtc)
    {
        _view = view;
        _languageCode = languageCode;
        if (Bounds != monitor.Bounds)
        {
            Bounds = monitor.Bounds;
        }

        var isNight = view.OverlayKind == OverlayKind.Night;
        var accent = isNight ? Theme.NightAccent : Theme.DayAccent;
        BackColor = isNight ? Theme.NightBackground : Theme.Background;
        _scrollHost.BackColor = BackColor;
        _content.BackColor = BackColor;
        _eyebrow.ForeColor = accent;
        _title.Text = UiText.Get(
            languageCode,
            isNight ? TextKey.NightTitle : TextKey.DayTitle);
        var detailTemplate = UiText.Get(
            languageCode,
            isNight ? TextKey.NightDetail : TextKey.DayDetail);
        _detail.Text = isNight && view.DeadlineUtc is { } nightEndUtc
            ? string.Format(
                UiText.Culture(languageCode),
                detailTemplate,
                nightEndUtc.ToLocalTime().ToString("HH:mm", UiText.Culture(languageCode)))
            : detailTemplate;
        _challengeReveal.Text = UiText.Get(
            languageCode,
            isNight ? TextKey.OverrideNight : TextKey.SkipDay);
        _challengeReveal.AccessibleName = _challengeReveal.Text;
        _challengeReveal.FlatAppearance.BorderColor = accent;

        _graceActions.Controls.Clear();
        if (isNight && view.NightGraceAvailable)
        {
            _nightGraceButton.Text = UiText.Get(languageCode, TextKey.NightGrace10);
            _graceActions.Controls.Add(_nightGraceButton);
        }
        else if (!isNight && view.DaySnoozeAvailable)
        {
            RefreshDayButtonText();
            _graceActions.Controls.AddRange([_snooze5, _snooze15, _snooze30]);
        }

        UpdateCountdown(nowUtc);
        PositionContent();
    }

    internal void UpdateCountdown(DateTimeOffset nowUtc)
    {
        var text = CountdownFormatter.FormatRemaining(_view.DeadlineUtc, nowUtc);
        if (string.Equals(_countdown.Text, text, StringComparison.Ordinal))
        {
            return;
        }

        _countdown.Text = text;
        _countdown.AccessibleDescription = _countdown.Text;
    }

    internal void SetEmergencyProgress(TimeSpan? remaining)
    {
        _emergency.Visible = remaining is not null;
        if (remaining is null)
        {
            _emergency.Text = string.Empty;
            return;
        }

        _emergency.Text = string.Format(
            UiText.Culture(_languageCode),
            UiText.Get(_languageCode, TextKey.EmergencyHold),
            Math.Max(0, remaining.Value.TotalSeconds));
        PositionContent();
    }

    internal void Reassert(MonitorDescriptor monitor)
    {
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopmost,
            monitor.Bounds.X,
            monitor.Bounds.Y,
            monitor.Bounds.Width,
            monitor.Bounds.Height,
            NativeMethods.SwpNoActivate |
            NativeMethods.SwpNoOwnerZOrder |
            NativeMethods.SwpAsyncWindowPos |
            NativeMethods.SwpShowWindow);
    }

    internal void AllowClose()
    {
        _allowClose = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs eventArgs)
    {
        if (!_allowClose)
        {
            eventArgs.Cancel = true;
        }

        base.OnFormClosing(eventArgs);
    }

    private Button CreateActionButton(TextKey key, int minutes)
    {
        var button = Theme.StyleButton(
            new Button
            {
                Text = UiText.Get(_languageCode, key),
                AccessibleName = UiText.Get(_languageCode, key)
            },
            Theme.DayAccent,
            filled: true);
        button.Click += (_, _) => _daySnooze(minutes);
        return button;
    }

    private void RefreshDayButtonText()
    {
        _snooze5.Text = UiText.Get(_languageCode, TextKey.Snooze5);
        _snooze15.Text = UiText.Get(_languageCode, TextKey.Snooze15);
        _snooze30.Text = UiText.Get(_languageCode, TextKey.Snooze30);
    }

    private void ShowChallenge()
    {
        if (_challenge is not null)
        {
            _challenge.Visible = true;
            _challenge.FocusInput();
            return;
        }

        var isNight = _view.OverlayKind == OverlayKind.Night;
        var accent = isNight ? Theme.NightAccent : Theme.DayAccent;
        _challenge = new ExactTypingPanel(
            isNight
                ? UiText.NightStatement(_languageCode)
                : UiText.DayStatement(_languageCode),
            _languageCode,
            BackColor,
            accent,
            showBackButton: true)
        {
            Dock = DockStyle.Top,
            Margin = new Padding(0, 18, 0, 0)
        };
        _challenge.Completed += () =>
        {
            if (isNight)
            {
                _nightOverride();
            }
            else
            {
                _daySkip();
            }
        };
        _challenge.Cancelled += HideChallenge;

        _content.Controls.Add(_challenge);
        _challengeReveal.Visible = false;
        _graceActions.Visible = false;
        PositionContent();
        _challenge.FocusInput();
    }

    private void HideChallenge()
    {
        if (_challenge is null)
        {
            return;
        }

        _challenge.ClearInput();
        _challenge.Visible = false;
        _challengeReveal.Visible = true;
        _graceActions.Visible = true;
        PositionContent();
        _challengeReveal.Focus();
    }

    private void PositionContent()
    {
        if (_scrollHost.ClientSize.Width <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(620, _scrollHost.ClientSize.Width - 80);
        _content.Width = Math.Min(920, availableWidth);
        _content.Left = Math.Max(40, (_scrollHost.ClientSize.Width - _content.Width) / 2);
        _content.Top = Math.Max(32, (_scrollHost.ClientSize.Height - _content.PreferredSize.Height) / 2);
    }
}
