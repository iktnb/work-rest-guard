using Microsoft.Win32;

namespace WorkRestGuard;

internal sealed class GuardApplicationContext : ApplicationContext
{
    private readonly LaunchOptions _options;
    private readonly FileLogger _logger;
    private readonly IClock _clock;
    private readonly AtomicJsonStore<GuardConfig> _configStore;
    private readonly AtomicJsonStore<GuardRuntimeState> _stateStore;
    private readonly HistoryStore _historyStore;
    private readonly MonitorCatalog _monitorCatalog;
    private readonly IActivitySource _activitySource;
    private readonly GuardEngine _engine;
    private readonly OverlayController _overlayController;
    private readonly EmergencyHotkeyWindow _emergencyHotkey;
    private readonly EventWaitHandle _showSettingsSignal;
    private readonly Control _dispatcher;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private GuardConfig _config;
    private SettingsForm? _settingsForm;
    private GuardPhase _lastPhase;
    private bool _monitorMissingNotified;
    private bool _disposed;

    internal GuardApplicationContext(
        LaunchOptions options,
        FileLogger logger,
        IClock? clock = null,
        IActivitySource? activitySource = null)
    {
        _options = options;
        _logger = logger;
        _clock = clock ?? new SystemClock();
        _activitySource = activitySource ?? new InputActivitySource();
        _configStore = new AtomicJsonStore<GuardConfig>(AppPaths.ConfigPath, logger);
        _stateStore = new AtomicJsonStore<GuardRuntimeState>(AppPaths.StatePath, logger);
        _historyStore = new HistoryStore(logger);
        _monitorCatalog = new MonitorCatalog(logger);

        _config = _configStore.Load();
        if (_config.SchemaVersion != 1)
        {
            logger.Warn("Unsupported configuration schema; starting unconfigured");
            _config = new GuardConfig();
        }

        _config.Normalize();
        var runtimeState = _stateStore.Load();
        _engine = new GuardEngine(
            _config,
            runtimeState,
            _clock,
            new ScheduleResolver());
        _engine.StateChanged += OnStateChanged;
        _engine.EventCreated += OnEventCreated;
        _lastPhase = runtimeState.Phase;

        _overlayController = new OverlayController(
            minutes => _engine.SnoozeDay(minutes),
            () => _engine.SkipDayBreak(),
            () => _engine.UseNightGrace(),
            () => _engine.OverrideNight());

        _dispatcher = new Control();
        _ = _dispatcher.Handle;

        _emergencyHotkey = new EmergencyHotkeyWindow();
        _emergencyHotkey.HoldProgressChanged += OnEmergencyProgressChanged;
        _emergencyHotkey.Completed += EmergencyExit;
        if (!_emergencyHotkey.Registered)
        {
            logger.Warn("Emergency hotkey could not be registered");
        }

        _showSettingsSignal = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            AppIdentity.ShowSettingsEventName,
            out _);

        _statusItem = new ToolStripMenuItem { Enabled = false };
        _trayIcon = new NotifyIcon
        {
            Text = AppIdentity.ProductName,
            Icon = TrayIconFactory.Create(runtimeState.Phase),
            ContextMenuStrip = BuildMenu(),
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings(openHistory: false);

        _timer = new System.Windows.Forms.Timer { Interval = 500 };
        _timer.Tick += (_, _) => OnTimerTick();
        _timer.Start();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        _activitySource.ResetBaseline();
        if (!_options.SafeMode && _config.IsConfigured && _config.GuardEnabled)
        {
            _engine.Initialize();
        }

        RefreshUserInterface();

        if (!_config.IsConfigured || !_options.Background || _options.SafeMode)
        {
            ShowSettings(options.OpenHistory);
        }
    }

    internal void EmergencyExit()
    {
        _engine.RecordEmergencyExit();
        _overlayController.Hide();
        ExitThread();
    }

    private ContextMenuStrip BuildMenu()
    {
        var language = _config.LanguageCode;
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(
            UiText.Get(language, TextKey.StartBreakNow),
            null,
            (_, _) => StartBreakNow());
        menu.Items.Add(
            UiText.Get(language, TextKey.OpenSettings),
            null,
            (_, _) => ShowSettings(openHistory: false));
        menu.Items.Add(
            UiText.Get(language, TextKey.OpenHistory),
            null,
            (_, _) => ShowSettings(openHistory: true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(
            UiText.Get(language, TextKey.Exit),
            null,
            (_, _) => RequestNormalExit());
        return menu;
    }

    private void RebuildMenu()
    {
        var old = _trayIcon.ContextMenuStrip;
        _trayIcon.ContextMenuStrip = BuildMenu();
        old?.Dispose();
    }

    private void OnTimerTick()
    {
        if (_showSettingsSignal.WaitOne(0))
        {
            if (_engine.View.ShowOverlay)
            {
                RefreshOverlay();
            }
            else
            {
                ShowSettings(openHistory: false);
            }
        }

        if (!_options.SafeMode && _config.IsConfigured && _config.GuardEnabled)
        {
            _engine.Tick(_activitySource.PollHasNewInput());
        }

        RefreshUserInterface();
    }

    private void RefreshUserInterface()
    {
        RefreshOverlay();
        RefreshTray();
    }

    private void RefreshOverlay()
    {
        if (_options.SafeMode || !_config.IsConfigured || !_config.GuardEnabled)
        {
            _overlayController.Hide();
            return;
        }

        var monitors = _monitorCatalog.GetMonitors();
        var monitor = _monitorCatalog.Find(monitors, _config.ProtectedMonitor);
        if (monitor is null)
        {
            _overlayController.Hide();
            if (!_monitorMissingNotified)
            {
                _monitorMissingNotified = true;
                ShowWarning(UiText.Get(_config.LanguageCode, TextKey.MonitorMissing));
            }

            return;
        }

        _monitorMissingNotified = false;
        var view = _engine.View;
        _overlayController.Update(
            view,
            monitor,
            _config.LanguageCode,
            _clock.UtcNow);
        _overlayController.Tick(view, monitor, _clock.UtcNow);
    }

    private void RefreshTray()
    {
        var state = _engine.State;
        var status = FormatStatus(state);
        _statusItem.Text = status;
        _trayIcon.Text = status.Length <= 63 ? status : status[..63];

        var oldIcon = _trayIcon.Icon;
        _trayIcon.Icon = TrayIconFactory.Create(state.Phase);
        oldIcon?.Dispose();
    }

    private string FormatStatus(GuardRuntimeState state)
    {
        if (_options.SafeMode)
        {
            return UiText.Get(_config.LanguageCode, TextKey.SafeMode);
        }

        string Format(TextKey key, DateTimeOffset? deadline)
        {
            var localTime = (deadline ?? _clock.UtcNow)
                .ToLocalTime()
                .ToString("t", UiText.Culture(_config.LanguageCode));
            return string.Format(
                UiText.Culture(_config.LanguageCode),
                UiText.Get(_config.LanguageCode, key),
                localTime);
        }

        return state.Phase switch
        {
            GuardPhase.Ready => UiText.Get(_config.LanguageCode, TextKey.StatusReady),
            GuardPhase.Working => Format(TextKey.StatusWorking, state.WorkDeadlineUtc),
            GuardPhase.DayResting => Format(TextKey.StatusDayResting, state.DayDeadlineUtc),
            GuardPhase.DaySnoozed => Format(TextKey.StatusDaySnoozed, state.DayDeadlineUtc),
            GuardPhase.NightLocked => Format(TextKey.StatusNightLocked, state.NightEndUtc),
            GuardPhase.NightGrace => Format(TextKey.StatusNightGrace, state.NightGraceDeadlineUtc),
            _ => AppIdentity.ProductName
        };
    }

    private void OnStateChanged(GuardRuntimeState state)
    {
        try
        {
            _stateStore.Save(state);
        }
        catch
        {
            ShowWarning("Runtime state could not be saved. Protection remains active.");
        }

        if (state.Phase == GuardPhase.Ready && _lastPhase != GuardPhase.Ready)
        {
            _activitySource.ResetBaseline();
        }

        _lastPhase = state.Phase;
        RefreshUserInterface();
    }

    private void OnEventCreated(GuardEvent guardEvent) =>
        _historyStore.Append(guardEvent, _clock.UtcNow);

    private void StartBreakNow()
    {
        if (_engine.StartManualBreak())
        {
            RefreshUserInterface();
        }
    }

    private void ShowSettings(bool openHistory)
    {
        if (_engine.View.ShowOverlay)
        {
            RefreshOverlay();
            return;
        }

        if (_settingsForm is not null && !_settingsForm.IsDisposed)
        {
            _settingsForm.Activate();
            return;
        }

        var monitors = _monitorCatalog.GetMonitors();
        _settingsForm = new SettingsForm(
            _config,
            monitors,
            _monitorCatalog,
            _historyStore,
            _clock,
            openHistory);
        var result = _settingsForm.ShowDialog();
        if (result == DialogResult.OK)
        {
            ApplySettings(_settingsForm.ResultConfig, _settingsForm.StartBreakRequested);
        }

        _settingsForm.Dispose();
        _settingsForm = null;
        RefreshUserInterface();
    }

    private void ApplySettings(GuardConfig candidate, bool startBreak)
    {
        try
        {
            StartupManager.SetEnabled(candidate.LaunchAtSignIn);
        }
        catch (Exception exception)
        {
            _logger.Error("Could not update Windows startup", exception);
            ShowWarning(UiText.Get(_config.LanguageCode, TextKey.StartupError));
            return;
        }

        try
        {
            _configStore.Save(candidate);
        }
        catch
        {
            return;
        }

        var languageChanged = !string.Equals(
            _config.LanguageCode,
            candidate.LanguageCode,
            StringComparison.OrdinalIgnoreCase);
        _config = candidate;
        _engine.ApplyConfig(candidate);
        if (languageChanged)
        {
            RebuildMenu();
        }

        if (startBreak)
        {
            _engine.StartManualBreak();
        }
    }

    private void RequestNormalExit()
    {
        using var challenge = new ChallengeForm(ChallengeKind.Night, _config.LanguageCode);
        if (challenge.ShowDialog() == DialogResult.OK)
        {
            _overlayController.Hide();
            ExitThread();
        }
    }

    private void OnEmergencyProgressChanged(TimeSpan? remaining)
    {
        _overlayController.SetEmergencyProgress(remaining);
        if (remaining is not null)
        {
            _trayIcon.Text = string.Format(
                UiText.Culture(_config.LanguageCode),
                UiText.Get(_config.LanguageCode, TextKey.EmergencyHold),
                Math.Max(0, remaining.Value.TotalSeconds));
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs eventArgs) =>
        Dispatch(RefreshUserInterface);

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs eventArgs)
    {
        if (eventArgs.Mode == PowerModes.Resume)
        {
            Dispatch(() =>
            {
                _engine.Initialize();
                RefreshUserInterface();
            });
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs eventArgs)
    {
        if (eventArgs.Reason == SessionSwitchReason.SessionUnlock)
        {
            Dispatch(() =>
            {
                _engine.Initialize();
                RefreshUserInterface();
            });
        }
    }

    private void Dispatch(Action action)
    {
        try
        {
            _dispatcher.BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // Application is shutting down.
        }
    }

    private void ShowWarning(string message)
    {
        _trayIcon.BalloonTipTitle = AppIdentity.ProductName;
        _trayIcon.BalloonTipText = message;
        _trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
        _trayIcon.ShowBalloonTip(4000);
    }

    protected override void ExitThreadCore()
    {
        _overlayController.Hide();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _timer.Stop();
            _timer.Dispose();
            _engine.StateChanged -= OnStateChanged;
            _engine.EventCreated -= OnEventCreated;
            _overlayController.Dispose();
            _emergencyHotkey.HoldProgressChanged -= OnEmergencyProgressChanged;
            _emergencyHotkey.Completed -= EmergencyExit;
            _emergencyHotkey.Dispose();
            _showSettingsSignal.Dispose();
            _dispatcher.Dispose();
            _settingsForm?.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Icon?.Dispose();
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
