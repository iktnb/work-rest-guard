using WorkRestGuard;

namespace OverlayHarness;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var night = args.Contains("--night", StringComparer.OrdinalIgnoreCase);
        var english = args.Contains("--en", StringComparer.OrdinalIgnoreCase);
        var durationSeconds = ParseDuration(args);

        using var logger = new FileLogger(
            Path.Combine(Path.GetTempPath(), "work-rest-guard-harness.log"));
        var catalog = new MonitorCatalog(logger);
        var monitor = MonitorCatalog.ChooseDefault(catalog.GetMonitors())
            ?? throw new InvalidOperationException("No monitor is available.");
        if (args.Contains("--settings", StringComparer.OrdinalIgnoreCase))
        {
            RunSettingsHarness(
                logger,
                catalog,
                monitor,
                english ? "en" : "uk",
                durationSeconds);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var view = new GuardViewState(
            night ? GuardPhase.NightLocked : GuardPhase.DayResting,
            ShowOverlay: true,
            night ? OverlayKind.Night : OverlayKind.Day,
            now.AddMinutes(night ? 600 : 15),
            DaySnoozeAvailable: !night,
            NightGraceAvailable: night,
            NightScheduleId: night ? DateTime.Today.ToString("yyyy-MM-dd") : null);

        using var form = new GuardOverlayForm(
            monitor,
            view,
            english ? "en" : "uk",
            _ => { },
            () => { },
            () => { },
            () => { },
            targetableForTesting: true);
        using var timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(1, durationSeconds) * 1000
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            form.AllowClose();
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
    }

    private static void RunSettingsHarness(
        FileLogger logger,
        MonitorCatalog catalog,
        MonitorDescriptor monitor,
        string languageCode,
        int durationSeconds)
    {
        var config = new GuardConfig
        {
            ProtectedMonitor = MonitorIdentity.FromDescriptor(monitor),
            LanguageCode = languageCode
        };
        var historyPath = Path.Combine(
            Path.GetTempPath(),
            "work-rest-guard-harness",
            Environment.ProcessId.ToString(),
            "history.json");
        var historyStore = new HistoryStore(logger, historyPath);
        using var form = new SettingsForm(
            config,
            catalog.GetMonitors(),
            catalog,
            historyStore,
            new SystemClock(),
            openHistory: false);
        using var timer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(1, durationSeconds) * 1000
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            form.Close();
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
    }

    private static int ParseDuration(string[] args)
    {
        var index = Array.FindIndex(
            args,
            value => string.Equals(value, "--seconds", StringComparison.OrdinalIgnoreCase));
        return index >= 0 &&
               index + 1 < args.Length &&
               int.TryParse(args[index + 1], out var result)
            ? result
            : 20;
    }
}
