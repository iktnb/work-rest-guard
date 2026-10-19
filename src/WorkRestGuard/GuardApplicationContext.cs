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
    private readonly StatusTimerController _statusTimerController = new();
    private readonly EventWaitHandle _showSettingsSignal;
    private readonly EventWaitHandle _updateStopSignal;
    private readonly Control _dispatcher = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem = new() { Enabled = false };
    private GuardConfig _config;
    private SettingsForm? _settingsForm;
    private GuardPhase _lastPhase;
    private GuardPhase _trayIconPhase;
    private GuardViewState? _renderedView;
    private IReadOnlyList<MonitorDescriptor>? _monitors;
    private DateTimeOffset _nextMonitorRefresh;
    private bool _disposed;

    internal GuardApplicationContext(LaunchOptions options, FileLogger logger, IClock? clock = null,
        IActivitySource? activitySource = null, string? dataDirectory = null, IUsageSource? usageSource = null)
    {
        _options = options;
        _logger = logger;
        _clock = clock ?? new SystemClock();
        _activitySource = activitySource ?? new InputActivitySource();
        var directory = dataDirectory ?? AppPaths.DataDirectory;
        _configStore = new(Path.Combine(directory, "config.json"), logger);
        _stateStore = new(Path.Combine(directory, "state.json"), logger);
        _historyStore = new(logger, Path.Combine(directory, "history.json"));
        _monitorCatalog = new(logger);
        _config = _configStore.Load();
        if (_config.SchemaVersion != 1) _config = new GuardConfig();
        _config.Normalize();
        _usage = new(new UsageStore(Path.Combine(directory, "usage")), logger,
            _config, _clock, options.SafeMode, usageSource);
        _engine = new(_config, _stateStore.Load(), _clock);
        _engine.StateChanged += OnStateChanged;
        _engine.EventCreated += OnEventCreated;
        _lastPhase = _engine.State.Phase;
        _ = _dispatcher.Handle;
        // Isolated harness instances cannot signal the installed application.
        _showSettingsSignal = new(false, EventResetMode.AutoReset,
            dataDirectory is null ? AppIdentity.ShowSettingsEventName : null);
        _updateStopSignal = new(false, EventResetMode.AutoReset,
            dataDirectory is null ? AppIdentity.UpdateStopEventName : null);
        _trayIconPhase = _engine.State.Phase;
        _trayIcon = new()
        {
            Text = AppIdentity.ProductName,
            Icon = TrayIconFactory.Create(_trayIconPhase),
            ContextMenuStrip = BuildMenu(),
            Visible = true
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowSettings(false);
        };
        _timer = new() { Interval = 500 };
        _timer.Tick += (_, _) => OnTimerTick();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
        _activitySource.ResetBaseline();
        _engine.Initialize();
        _usage.Capture(_engine.State.Phase);
        _timer.Start();
        if (!_config.IsConfigured || !options.Background || options.SafeMode)
            _dispatcher.BeginInvoke(() => ShowSettings(options.OpenHistory, options.OpenStatistics));
    }

    internal void RequestMaintenanceStop() => _updateStopSignal.Set();
    internal void FlushUsage() => _usage.Flush();
    internal IReadOnlyList<string> MenuLabels => _trayIcon.ContextMenuStrip!.Items
        .OfType<ToolStripMenuItem>().Select(item => item.Text ?? string.Empty).ToArray();

    internal void RecoverUiAfterError()
    {
        _usage.Flush();
        _renderedView = null;
        _statusTimerController.Hide();
    }

    private ContextMenuStrip BuildMenu()
    {
        var language = _config.LanguageCode;
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(UsageText.Pick(language, "Статистика часу…", "Time statistics…"),
            null, (_, _) => ShowSettings(false, true));
        menu.Items.Add(UiText.Get(language, TextKey.StartBreakNow), null, (_, _) =>
        {
            if (!_options.SafeMode) _engine.StartManualBreak();
        });
        menu.Items.Add(UiText.Get(language, TextKey.OpenSettings), null, (_, _) => ShowSettings(false));
        menu.Items.Add(UiText.Get(language, TextKey.OpenHistory), null, (_, _) => ShowSettings(true));
        return menu;
    }

    private void OnTimerTick()
    {
        _usage.Capture(_engine.State.Phase);
        if (_updateStopSignal.WaitOne(0))
        {
            ExitThread();
            return;
        }
        if (_showSettingsSignal.WaitOne(0)) ShowSettings(false);
        if (!_options.SafeMode && _config.IsConfigured)
            _engine.Tick(_activitySource.PollHasNewInput());
        RefreshUserInterface();
    }

    private void RefreshUserInterface()
    {
        var state = _engine.State;
        var key = state.Phase switch
        {
            GuardPhase.Working => TextKey.StatusWorking,
            GuardPhase.DayResting => TextKey.StatusDayResting,
            _ => TextKey.StatusReady
        };
        var status = _options.SafeMode ? UiText.Get(_config.LanguageCode, TextKey.SafeMode) :
            string.Format(UiText.Culture(_config.LanguageCode), UiText.Get(_config.LanguageCode, key),
                CountdownFormatter.FormatRemaining(_engine.View.DeadlineUtc, _clock.UtcNow));
        _statusItem.Text = status;
        _trayIcon.Text = status.Length <= 63 ? status : status[..63];
        if (_trayIconPhase != state.Phase)
        {
            var old = _trayIcon.Icon;
            _trayIcon.Icon = TrayIconFactory.Create(state.Phase);
            _trayIconPhase = state.Phase;
            old?.Dispose();
        }
        var now = _clock.UtcNow;
        var view = _engine.View;
        if (_options.SafeMode || !TimerPolicy.ShouldShowTimer(now, _config, view))
        {
            _statusTimerController.Hide();
            _renderedView = null;
            return;
        }
        if (_monitors is null || now >= _nextMonitorRefresh)
        {
            _monitors = _monitorCatalog.GetMonitors();
            _nextMonitorRefresh = now.AddSeconds(30);
            _renderedView = null;
        }
        var monitor = _monitorCatalog.Find(_monitors, _config.ProtectedMonitor);
        if (monitor is null)
        {
            _statusTimerController.Hide();
            _renderedView = null;
            return;
        }
        if (_renderedView != view)
        {
            _statusTimerController.Update(view, monitor, _config.LanguageCode, now);
            _renderedView = view;
        }
        else _statusTimerController.Tick(view, monitor, _config.LanguageCode, now);
    }

    private void OnStateChanged(GuardRuntimeState state)
    {
        try { _stateStore.Save(state); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowWarning(UsageText.Pick(_config.LanguageCode,
                "Не вдалося зберегти стан таймера.", "The timer state could not be saved."));
        }
        if (state.Phase == GuardPhase.Ready && _lastPhase != GuardPhase.Ready)
            _activitySource.ResetBaseline();
        _lastPhase = state.Phase;
    }

    private void OnEventCreated(GuardEvent guardEvent) => _historyStore.Append(guardEvent, _clock.UtcNow);

    private void ShowSettings(bool openHistory, bool openStatistics = false)
    {
        if (_settingsForm is { IsDisposed: false })
        {
            if (openStatistics) _settingsForm.ShowStatistics();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.Show();
            _settingsForm.Activate();
            return;
        }
        var monitors = _monitorCatalog.GetMonitors();
        using var form = new SettingsForm(_config, monitors, _monitorCatalog, _historyStore,
            _clock, openHistory, _usage, openStatistics);
        _settingsForm = form;
        try
        {
            if (form.ShowDialog() == DialogResult.OK)
                ApplySettings(form.ResultConfig, form.StartBreakRequested);
        }
        finally { _settingsForm = null; }
    }

    private void ApplySettings(GuardConfig candidate, bool startBreak)
    {
        candidate.Normalize();
        try { StartupManager.SetEnabled(candidate.LaunchAtSignIn); }
        catch (Exception exception)
        {
            _logger.Error("Could not update Windows startup", exception);
            ShowWarning(UiText.Get(_config.LanguageCode, TextKey.StartupError));
            return;
        }
        try { _configStore.Save(candidate); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return; }
        _config = candidate;
        _usage.ApplyConfig(candidate, _engine.State.Phase);
        _engine.ApplyConfig(candidate);
        var old = _trayIcon.ContextMenuStrip;
        _trayIcon.ContextMenuStrip = BuildMenu();
        old?.Dispose();
        _renderedView = null;
        _monitors = null;
        if (startBreak && !_options.SafeMode) _engine.StartManualBreak();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => Dispatch(() =>
    {
        _monitors = null;
        RefreshUserInterface();
    });

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            try { _dispatcher.Invoke(() => _usage.SetSuspended(true, _engine.State.Phase)); }
            catch (InvalidOperationException) { /* Already shutting down. */ }
        }
        if (e.Mode == PowerModes.Resume) Dispatch(() =>
        {
            _usage.SetSuspended(false, _engine.State.Phase);
            _engine.Initialize();
        });
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)
            Dispatch(() => _usage.SetLocked(true, _engine.State.Phase));
        else if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect)
            Dispatch(() =>
            {
                _usage.SetLocked(false, _engine.State.Phase);
                _engine.Initialize();
            });
    }

    private void Dispatch(Action action)
    {
        try { _dispatcher.BeginInvoke(action); }
        catch (InvalidOperationException) { /* Already shutting down. */ }
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
        _statusTimerController.Hide();
        _settingsForm?.Close();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
            _usage.Stop(_engine.State.Phase);
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _engine.StateChanged -= OnStateChanged;
            _engine.EventCreated -= OnEventCreated;
            _statusTimerController.Dispose();
            _showSettingsSignal.Dispose();
            _updateStopSignal.Dispose();
            _settingsForm?.Dispose();
            _dispatcher.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Icon?.Dispose();
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
