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
    private readonly UsageTrackingService _usage;
    private readonly MonitorCatalog _monitorCatalog;
    private readonly IActivitySource _activitySource;
    private readonly GuardEngine _engine;
    private readonly OverlayController _overlayController;
    private readonly StatusTimerController _statusTimerController;
    private readonly EmergencyHotkeyWindow _emergencyHotkey;
    private readonly EventWaitHandle _showSettingsSignal;
    private readonly Control _dispatcher;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private GuardConfig _config;
    private SettingsForm? _settingsForm;
    private GuardPhase _lastPhase;
    private GuardPhase _trayIconPhase;
    private GuardViewState _lastView;
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
        _usage = new UsageTrackingService(new UsageStore(AppPaths.UsageDirectory), logger,
            _config, _clock, options.SafeMode);
        var runtimeState = _stateStore.Load();
        _engine = new GuardEngine(
            _config,
            runtimeState,
            _clock,
            new ScheduleResolver());
        _lastView = _engine.View;
        _engine.StateChanged += OnStateChanged;
        _engine.EventCreated += OnEventCreated;
        _lastPhase = runtimeState.Phase;

        _overlayController = new OverlayController(
            minutes => _engine.SnoozeDay(minutes),
            () => _engine.SkipDayBreak(),
            () => _engine.UseNightGrace(),
            () => _engine.OverrideNight());
        _statusTimerController = new StatusTimerController();

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
        _trayIconPhase = runtimeState.Phase;
        _trayIcon = new NotifyIcon
        {
            Text = AppIdentity.ProductName,
            Icon = TrayIconFactory.Create(runtimeState.Phase),
            ContextMenuStrip = BuildMenu(),
            Visible = true
        };
        _trayIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                ShowSettings(openHistory: false);
            }
        };

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

        _usage.Capture(_engine.State.Phase);

        if (!_config.IsConfigured || !_options.Background || _options.SafeMode)
        {
            ShowSettings(options.OpenHistory, options.OpenStatistics);
        }
    }

    internal void EmergencyExit()
    {
        _engine.RecordEmergencyExit();
        _overlayController.Hide();
        _statusTimerController.Hide();
        ExitThread();
    }

    private ContextMenuStrip BuildMenu()
    {
        var language = _config.LanguageCode;
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(
            UsageText.Pick(language, "Статистика часу…", "Time statistics…"),
            null,
            (_, _) => ShowSettings(openHistory: false, openStatistics: true));
        menu.Items.Add(
            _config.UsageTrackingEnabled
                ? UsageText.Pick(language, "Призупинити збір статистики", "Pause statistics recording")
                : UsageText.Pick(language, "Відновити збір статистики", "Resume statistics recording"),
            null,
            (_, _) => ToggleUsageTracking());
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
            ShowSettings(openHistory: false);
        }

        if (!_options.SafeMode && _config.IsConfigured && _config.GuardEnabled)
        {
            _engine.Tick(_activitySource.PollHasNewInput());
        }

        _usage.Capture(_engine.State.Phase);

        RefreshLiveUserInterface();
    }

    private void RefreshUserInterface()
    {
        RefreshMonitorSurfaces(rebuild: true);
        RefreshTray();
    }

    private void RefreshLiveUserInterface()
    {
        RefreshMonitorSurfaces(rebuild: false);
        RefreshTray();
    }

    private void RefreshMonitorSurfaces(bool rebuild)
    {
        if (_options.SafeMode || !_config.IsConfigured || !_config.GuardEnabled)
        {
            _overlayController.Hide();
            _statusTimerController.Hide();
            return;
        }

        var monitors = _monitorCatalog.GetMonitors();
        var monitor = _monitorCatalog.Find(monitors, _config.ProtectedMonitor);
        if (monitor is null)
        {
            _overlayController.Hide();
            _statusTimerController.Hide();
            if (!_monitorMissingNotified)
            {
                _monitorMissingNotified = true;
                ShowWarning(UiText.Get(_config.LanguageCode, TextKey.MonitorMissing));
            }

            return;
        }

        _monitorMissingNotified = false;
        var view = _engine.View;
        var now = _clock.UtcNow;
        if (rebuild)
        {
            _overlayController.Update(
                view,
                monitor,
                _config.LanguageCode,
                now);
            _statusTimerController.Update(
                view,
                monitor,
                _config.LanguageCode,
                now);
        }
        else
        {
            _overlayController.Tick(view, monitor, now);
            _statusTimerController.Tick(view, monitor, now);
        }
    }

    private void RefreshTray()
    {
        var state = _engine.State;
        var status = FormatStatus(state);
        _statusItem.Text = status;
        _trayIcon.Text = status.Length <= 63 ? status : status[..63];

        if (_trayIconPhase != state.Phase)
        {
            var oldIcon = _trayIcon.Icon;
            _trayIcon.Icon = TrayIconFactory.Create(state.Phase);
            _trayIconPhase = state.Phase;
            oldIcon?.Dispose();
        }
    }

    private string FormatStatus(GuardRuntimeState state)
    {
        if (_options.SafeMode)
        {
            return UiText.Get(_config.LanguageCode, TextKey.SafeMode);
        }

        string Format(TextKey key, DateTimeOffset? deadline)
        {
            return string.Format(
                UiText.Culture(_config.LanguageCode),
                UiText.Get(_config.LanguageCode, key),
                CountdownFormatter.FormatRemaining(deadline, _clock.UtcNow));
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
        var nextView = _engine.View;
        var viewChanged = nextView != _lastView;
        _lastView = nextView;

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
        if (viewChanged)
        {
            RefreshUserInterface();
        }
        else
        {
            RefreshLiveUserInterface();
        }
    }

    private void OnEventCreated(GuardEvent guardEvent) =>
        _historyStore.Append(guardEvent, _clock.UtcNow);

    private void StartBreakNow()
    {
        if (_options.SafeMode)
        {
            ShowWarning(UiText.Get(_config.LanguageCode, TextKey.SafeModeBreakBlocked));
            return;
        }

        if (_engine.StartManualBreak())
        {
            RefreshUserInterface();
        }
    }

    private void ShowSettings(bool openHistory, bool openStatistics = false)
    {
        if (_settingsForm is not null && !_settingsForm.IsDisposed)
        {
            if (openStatistics)
            {
                _settingsForm.ShowStatistics();
            }
            if (_settingsForm.WindowState == FormWindowState.Minimized)
            {
                _settingsForm.WindowState = FormWindowState.Normal;
            }

            if (!_settingsForm.Visible)
            {
                _settingsForm.Show();
            }
            _settingsForm.BringToFront();
            _settingsForm.Activate();
            return;
        }

        var monitors = _monitorCatalog.GetMonitors();
        var settingsForm = new SettingsForm(
            _config,
            monitors,
            _monitorCatalog,
            _historyStore,
            _clock,
            openHistory,
            _usage,
            openStatistics);
        _settingsForm = settingsForm;
        PositionSettingsOnAvailableMonitor(settingsForm, monitors);
        settingsForm.Shown += (_, _) =>
            settingsForm.BeginInvoke(() =>
                PositionSettingsOnAvailableMonitor(
                    settingsForm,
                    _monitorCatalog.GetMonitors()));
        var result = settingsForm.ShowDialog();
        if (result == DialogResult.OK)
        {
            ApplySettings(settingsForm.ResultConfig, settingsForm.StartBreakRequested);
        }

        settingsForm.Dispose();
        _settingsForm = null;
        RefreshUserInterface();
    }

    private void PositionSettingsOnAvailableMonitor(
        Form settings,
        IReadOnlyList<MonitorDescriptor> monitors)
    {
        var protectedMonitor = _monitorCatalog.Find(monitors, _config.ProtectedMonitor);
        var available = monitors
            .Where(monitor =>
                protectedMonitor is null ||
                !string.Equals(
                    monitor.DeviceName,
                    protectedMonitor.DeviceName,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
        var target = available.FirstOrDefault(monitor =>
                monitor.WorkingArea.Contains(Cursor.Position))
            ?? available.FirstOrDefault()
            ?? protectedMonitor
            ?? MonitorCatalog.ChooseDefault(monitors);
        if (target is null)
        {
            _logger.Warn("Settings window could not be assigned to a monitor");
            return;
        }

        settings.StartPosition = FormStartPosition.Manual;
        var area = target.WorkingArea;
        var location = new Point(
            area.Left + Math.Max(0, (area.Width - settings.Width) / 2),
            area.Top + Math.Max(0, (area.Height - settings.Height) / 2));
        settings.DesktopLocation = location;
        var moved = true;
        if (settings.IsHandleCreated)
        {
            moved = NativeMethods.SetWindowPos(
                settings.Handle,
                0,
                location.X,
                location.Y,
                settings.Width,
                settings.Height,
                NativeMethods.SwpNoZOrder |
                NativeMethods.SwpNoActivate |
                NativeMethods.SwpNoOwnerZOrder);
        }

        _logger.Info(
            $"Settings monitor: protected={protectedMonitor?.DeviceName ?? "unresolved"}, " +
            $"target={target.DeviceName}, requested={location.X},{location.Y}, " +
            $"effective={settings.Left},{settings.Top}, native={moved}");
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

        _config = candidate;
        _usage.ApplyConfig(candidate, _engine.State.Phase);
        _engine.ApplyConfig(candidate);
        RebuildMenu();

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

    private void ToggleUsageTracking()
    {
        var candidate = _config.Clone();
        candidate.UsageTrackingEnabled = !candidate.UsageTrackingEnabled;
        try
        {
            _configStore.Save(candidate);
        }
        catch
        {
            ShowWarning(UsageText.Pick(_config.LanguageCode,
                "Не вдалося зберегти налаштування статистики.", "Statistics settings could not be saved."));
            return;
        }

        _config = candidate;
        _usage.ApplyConfig(candidate, _engine.State.Phase);
        _settingsForm?.UpdateUsageTracking(candidate.UsageTrackingEnabled);
        RebuildMenu();
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
        if (eventArgs.Mode == PowerModes.Suspend)
        {
            // Persist before Windows suspends the message loop.
            try
            {
                _dispatcher.Invoke(() => _usage.SetSuspended(true, _engine.State.Phase));
            }
            catch (InvalidOperationException)
            {
                // The application is already shutting down.
            }
        }

        if (eventArgs.Mode == PowerModes.Resume)
        {
            Dispatch(() =>
            {
                _usage.SetSuspended(false, _engine.State.Phase);
                _engine.Initialize();
                RefreshUserInterface();
            });
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs eventArgs)
    {
        if (eventArgs.Reason is SessionSwitchReason.SessionLock or
            SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)
        {
            Dispatch(() => _usage.SetLocked(true, _engine.State.Phase));
        }

        if (eventArgs.Reason == SessionSwitchReason.SessionUnlock)
        {
            Dispatch(() =>
            {
                _usage.SetLocked(false, _engine.State.Phase);
                _engine.Initialize();
                RefreshUserInterface();
            });
        }

        else if (eventArgs.Reason is SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect)
        {
            Dispatch(() => _usage.SetLocked(false, _engine.State.Phase));
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
        _usage.Stop(_engine.State.Phase);
        _overlayController.Hide();
        _statusTimerController.Hide();
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
            _usage.Stop(_engine.State.Phase);
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _timer.Stop();
            _timer.Dispose();
            _engine.StateChanged -= OnStateChanged;
            _engine.EventCreated -= OnEventCreated;
            _overlayController.Dispose();
            _statusTimerController.Dispose();
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
