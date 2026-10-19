using System.ComponentModel;
using System.Text.Json;
using WorkRestGuard;

namespace OverlayHarness;

internal static class StatisticsHarness
{
    internal static void Run(string[] args)
    {
        var directory = Path.Combine(Path.GetTempPath(), "work-rest-statistics-harness", Guid.NewGuid().ToString("N"));
        using var logger = new FileLogger(Path.Combine(directory, "guard.log"));
        var date = DateOnly.FromDateTime(DateTime.Today);
        var clock = new PreviewClock(UsageReport.LocalDayStart(date, TimeZoneInfo.Local).AddHours(21));
        var store = new UsageStore(Path.Combine(directory, "usage"));
        var language = args.Contains("--en") ? "en" : "uk";
        if (!args.Contains("--empty") && !args.Contains("--error"))
        {
            for (var offset = -6; offset <= 0; offset++)
            {
                var start = UsageReport.LocalDayStart(date.AddDays(offset), TimeZoneInfo.Local);
                Add(9, 9.8, UsageKind.Active, "code.exe");
                Add(9.8, 10.2, UsageKind.Active, "chrome.exe");
                Add(10.2, 10.5, UsageKind.Idle);
                Add(10.5, 12, UsageKind.Active, "code.exe");
                Add(12, 13, UsageKind.Locked);
                Add(13, 14, UsageKind.Suspended);
                Add(14, 14.5, UsageKind.Active, "obsidian.exe");
                Add(15, 17, UsageKind.Active, args.Contains("--long-names") ? new string('a', 130) + ".exe" : "game.exe");
                Add(17, 17.1, UsageKind.Unknown);
                Add(20, 20.5, UsageKind.Active, "chrome.exe", night: true);

                void Add(double from, double to, UsageKind kind, string? app = null, bool night = false) =>
                    store.Append(new UsageSpan(start.AddHours(from), start.AddHours(to), kind, app, GuardPhase.Working, night));
            }

            store.Flush(clock.UtcNow);
        }

        if (args.Contains("--error"))
        {
            Directory.CreateDirectory(Path.Combine(directory, "usage"));
            File.WriteAllText(Path.Combine(directory, "usage", date.ToString("yyyy-MM-dd") + ".json"), "{broken");
        }

        var service = new UsageTrackingService(store, logger, new GuardConfig(), clock);
        using var standalone = new StatisticsControl(service, clock, language, 5);
        using var preview = new PreviewForm
        {
            Text = "Work Rest Guard — Statistics preview (synthetic data)",
            Size = new Size(args.Contains("--narrow") ? 820 : 1100, args.Contains("--narrow") ? 680 : 840),
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.Black,
            Font = new Font("Segoe UI", args.Contains("--large-text") ? 13 : 10),
            AutoScaleMode = AutoScaleMode.Dpi,
            ShowInTaskbar = false,
            Opacity = args.Contains("--show") ? 1 : 0
        };
        preview.Controls.Add(standalone);
        using var settings = args.Contains("--in-settings") ? CreateSettings() : null;
        var form = (Form?)settings ?? preview;
        var control = Descendants(form).OfType<StatisticsControl>().Single();
        if (settings is not null && settings.AcceptButton is not null)
        {
            throw new InvalidOperationException("Enter in statistics must not save hidden settings.");
        }
        form.Size = preview.Size;
        form.ShowInTaskbar = false;
        form.Opacity = args.Contains("--show") ? 1 : 0;
        using var timer = new System.Windows.Forms.Timer { Interval = 300 };
        var stage = 0;
        timer.Tick += (_, _) =>
        {
            if (stage++ == 0)
            {
                var children = Descendants(control).ToList();
                var period = children.OfType<ComboBox>().Single();
                period.SelectedIndex = 1;
                period.SelectedIndex = 2;
                period.SelectedIndex = args.Contains("--week") ? 1 : 0;
                var views = children.OfType<TabControl>().Single();
                views.SelectedIndex = args.Contains("--timeline") ? 2 : args.Contains("--days") ? 1 : 0;
                var grids = children.OfType<DataGridView>().ToList();
                var applications = grids.Single(grid => grid.Columns.Contains("share"));
                applications.Sort(applications.Columns["minutes"]!, ListSortDirection.Descending);
                var values = applications.Rows.Cast<DataGridViewRow>().Select(row => Convert.ToDouble(row.Cells["minutes"].Value)).ToList();
                if (!values.SequenceEqual(values.OrderDescending()))
                {
                    throw new InvalidOperationException("Numeric duration sorting failed.");
                }

                var focusable = children.Where(child => child is ComboBox or DateTimePicker or Button or DataGridView).ToList();
                if (focusable.Any(child => string.IsNullOrWhiteSpace(child.AccessibleName)))
                {
                    throw new InvalidOperationException("A statistics control lacks an accessible name.");
                }

                var traversal = new List<string>();
                for (var index = 0; index < 6; index++)
                {
                    control.SelectNextControl(control.ActiveControl, true, true, true, true);
                    var active = control.ActiveControl;
                    while (active is ContainerControl container && container.ActiveControl is not null)
                    {
                        active = container.ActiveControl;
                    }

                    traversal.Add(active?.GetType().Name ?? "none");
                }

                if (!traversal.Contains(nameof(DateTimePicker)) || !traversal.Contains(nameof(ComboBox)) || !traversal.Contains(nameof(Button)))
                {
                    throw new InvalidOperationException("Statistics filters are not reachable through tab traversal.");
                }
                var capture = Option(args, "--capture");
                if (capture is not null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(capture, System.Drawing.Imaging.ImageFormat.Png);
                    File.WriteAllText(capture + ".json", JsonSerializer.Serialize(new
                    {
                        applicationRows = applications.Rows.Count,
                        dailyRows = grids.Single(grid => grid.Columns.Contains("date")).Rows.Count,
                        accessibleControls = focusable.Count,
                        selectedControl = form.ActiveControl?.GetType().Name,
                        tabTraversal = traversal,
                        numericSort = true,
                        filtersExercised = new[] { 1, 7, 30 },
                        syntheticData = true
                    }));
                }

                return;
            }

            timer.Stop();
            form.Close();
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
        Directory.Delete(directory, recursive: true);

        SettingsForm CreateSettings()
        {
            var catalog = new MonitorCatalog(logger);
            var monitors = catalog.GetMonitors();
            var monitor = MonitorCatalog.ChooseDefault(monitors) ?? throw new InvalidOperationException("No monitor.");
            return new SettingsForm(new GuardConfig
            {
                ProtectedMonitor = MonitorIdentity.FromDescriptor(monitor),
                LanguageCode = language
            }, monitors, catalog, new HistoryStore(logger, Path.Combine(directory, "history.json")), clock,
                openHistory: false, service, openStatistics: true);
        }
    }

    internal static void Probe(string[] args)
    {
        var directory = Path.Combine(Path.GetTempPath(), "work-rest-usage-probe", Guid.NewGuid().ToString("N"));
        using var logger = new FileLogger(Path.Combine(directory, "guard.log"));
        var clock = new SystemClock();
        var start = clock.UtcNow;
        var store = new UsageStore(Path.Combine(directory, "usage"));
        var service = new UsageTrackingService(store, logger, new GuardConfig { GuardEnabled = false }, clock);
        using var form = new PreviewForm { ShowInTaskbar = false, Opacity = 0 };
        using var timer = new System.Windows.Forms.Timer { Interval = 500 };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        timer.Tick += (_, _) =>
        {
            service.Capture(GuardPhase.Ready);
            if (stopwatch.Elapsed >= TimeSpan.FromSeconds(12))
            {
                service.Stop(GuardPhase.Ready);
                var spans = store.Read(start, clock.UtcNow);
                // Inspect foreground identity independently of idle classification;
                // this diagnostic threshold is never used by the application.
                var native = new WindowsUsageSource().Capture(int.MaxValue);
                var path = Option(args, "--capture") ?? throw new ArgumentException("Probe requires --capture.");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                File.WriteAllText(path, JsonSerializer.Serialize(new
                {
                    secondsObserved = spans.Sum(span => span.Duration.TotalSeconds),
                    intervalCount = spans.Count,
                    foregroundAttributed = spans.Any(span => span.Application is not null),
                    capturedStates = spans.Select(span => span.Kind.ToString()).Distinct().ToArray(),
                    foregroundLookupWorks = native.Kind == UsageKind.Active && native.Application is not null,
                    foregroundLookupState = native.Kind.ToString(),
                    noStorageError = !service.HasError,
                    temporaryStore = true
                }));
                timer.Stop();
                form.Close();
            }
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
        Directory.Delete(directory, recursive: true);
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private sealed class PreviewClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class PreviewForm : Form
    {
        protected override bool ShowWithoutActivation => true;
    }
}
