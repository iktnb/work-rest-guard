using System.Diagnostics;
using System.Text.Json;
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
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        if (args.Contains("--statistics")) { StatisticsHarness.Run(args); return; }
        if (args.Contains("--usage-probe")) { StatisticsHarness.Probe(args); return; }
        var directory = Path.Combine(Path.GetTempPath(), "work-rest-reminder-harness", Guid.NewGuid().ToString("N"));
        using var logger = new FileLogger(Path.Combine(directory, "guard.log"));
        var catalog = new MonitorCatalog(logger);
        var monitors = catalog.GetMonitors();
        var monitor = MonitorCatalog.ChooseDefault(monitors) ?? throw new InvalidOperationException("No monitor.");
        var config = new GuardConfig { ProtectedMonitor = MonitorIdentity.FromDescriptor(monitor), LanguageCode = args.Contains("--en") ? "en" : "uk" };
        if (args.Contains("--lifecycle")) { RunLifecycle(args, directory, logger, config); return; }
        var now = DateTimeOffset.UtcNow;
        using Form form = args.Contains("--settings")
            ? new SettingsForm(config, monitors, catalog, new HistoryStore(logger, Path.Combine(directory, "history.json")), new SystemClock(), false)
            : new StatusTimerForm(monitor, new GuardViewState(args.Contains("--rest") ? GuardPhase.DayResting : GuardPhase.Working,
                false, null, now.AddMinutes(args.Contains("--rest") ? 15 : 90), false, false, null), config.LanguageCode, now);
        form.ShowInTaskbar = false;
        form.Opacity = args.Contains("--show") ? 1 : 0;
        using var timer = new System.Windows.Forms.Timer { Interval = 350 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var capture = Option(args, "--capture");
            if (capture is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(capture, System.Drawing.Imaging.ImageFormat.Png);
            }
            form.Close();
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
    }

    private static void RunLifecycle(string[] args, string directory, FileLogger logger, GuardConfig config)
    {
        // A real message loop and elapsed clock cross 20:00 while collection continues.
        var local = DateTime.Today.AddHours(19).AddMinutes(59).AddSeconds(53);
        var start = new DateTimeOffset(local).ToUniversalTime();
        var clock = new ElapsedClock(start);
        new AtomicJsonStore<GuardConfig>(Path.Combine(directory, "config.json"), logger).Save(config);
        new AtomicJsonStore<GuardRuntimeState>(Path.Combine(directory, "state.json"), logger).Save(new()
        {
            Phase = GuardPhase.DayResting,
            LastHeartbeatUtc = start,
            DayDeadlineUtc = start.AddMinutes(15)
        });
        using var context = new GuardApplicationContext(new(false, false, false, false), logger, clock,
            new FakeActivity(), directory, new FakeSource());
        var menu = context.MenuLabels;
        if (menu.Any(label => label.Contains("Вийти") || label.Contains("Exit") || label.Contains("Призупинити") || label.Contains("Pause")))
            throw new InvalidOperationException("Tray must not offer exit or pause recording.");
        var settingsClosed = false;
        var daytimeReminder = false;
        var quietTimeHidden = false;
        var recovered = false;
        using var timer = new System.Windows.Forms.Timer { Interval = 250 };
        timer.Tick += (_, _) =>
        {
            var elapsed = clock.Elapsed.TotalSeconds;
            var forms = Application.OpenForms.Cast<Form>().ToArray();
            if (forms.Any(f => f is not SettingsForm and not StatusTimerForm))
                throw new InvalidOperationException("Unexpected enforcement window.");
            if (elapsed > 1 && !settingsClosed)
            {
                var settings = forms.OfType<SettingsForm>().Single();
                settings.Close();
                settingsClosed = true;
            }
            if (elapsed is > 2 and < 6)
            {
                var reminder = forms.OfType<StatusTimerForm>().SingleOrDefault();
                daytimeReminder |= reminder is not null && reminder.Width < 600 && reminder.Height < 200;
            }
            if (elapsed > 8)
            {
                quietTimeHidden = !forms.OfType<StatusTimerForm>().Any();
                if (!quietTimeHidden) throw new InvalidOperationException("Quiet hours must hide the compact timer.");
                if (!recovered) { context.RecoverUiAfterError(); recovered = true; }
            }
            if (elapsed > 13) { timer.Stop(); context.RequestMaintenanceStop(); }
        };
        timer.Start();
        Application.Run(context);
        var spans = new UsageStore(Path.Combine(directory, "usage")).Read(start, clock.UtcNow);
        var observed = spans.Sum(span => span.Duration.TotalSeconds);
        if (!settingsClosed || !daytimeReminder || !quietTimeHidden || !recovered || observed < 12 || !spans.Any(s => s.IsNightWindow))
            throw new InvalidOperationException("Lifecycle/collection verification failed.");
        var path = Option(args, "--capture") ?? throw new ArgumentException("--capture required.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            settingsClosed,
            daytimeReminder,
            quietTimeHidden,
            recovered,
            observedSeconds = observed,
            nightIntervals = spans.Count(s => s.IsNightWindow),
            maintenanceFlush = true,
            menu,
            isolatedSyntheticData = true
        }));
    }

    private static string? Option(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
    private sealed class ElapsedClock(DateTimeOffset start) : IClock
    {
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();
        internal TimeSpan Elapsed => _elapsed.Elapsed;
        public DateTimeOffset UtcNow => start.Add(_elapsed.Elapsed);
    }
    private sealed class FakeActivity : IActivitySource
    {
        public bool PollHasNewInput() => true;
        public void ResetBaseline() { }
    }
    private sealed class FakeSource : IUsageSource
    {
        public UsageSnapshot Capture(int idleMinutes) => new(UsageKind.Active, "synthetic.exe");
    }
}
