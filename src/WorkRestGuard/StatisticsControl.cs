namespace WorkRestGuard;

internal sealed class StatisticsControl : UserControl
{
    private readonly UsageTrackingService _service;
    private readonly IClock _clock;
    private readonly string _language;
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Custom, Width = 145 };
    private readonly ComboBox _period = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175 };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Label _coverage = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Dictionary<UsageKind, Label> _metrics = [];
    private readonly DataGridView _applications = CreateGrid();
    private readonly DataGridView _days = CreateGrid();
    private readonly DataGridView _timeline = CreateGrid();
    private bool _ready;

    internal StatisticsControl(UsageTrackingService service, IClock clock, string language, int idleMinutes)
    {
        _service = service;
        _clock = clock;
        _language = language;
        foreach (var grid in new[] { _applications, _days, _timeline })
        {
            grid.DefaultCellStyle.FormatProvider = UiText.Culture(language);
        }
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        AccessibleName = T("Статистика часу", "Time statistics");

        var today = TimeZoneInfo.ConvertTime(clock.UtcNow, TimeZoneInfo.Local).DateTime.Date;
        _date.MaxDate = today;
        _date.CustomFormat = UiText.Culture(language).DateTimeFormat.ShortDatePattern;
        _date.Value = today;
        _date.AccessibleName = T("Останній день періоду", "Period end date");
        _period.AccessibleName = T("Період статистики", "Statistics period");
        _period.Items.AddRange([T("Вибраний день", "Selected day"), T("Останні 7 днів", "Last 7 days"), T("Останні 30 днів", "Last 30 days")]);
        _period.SelectedIndex = 0;

        var refresh = Theme.StyleButton(new Button
        {
            Text = T("Оновити", "Refresh"),
            AccessibleName = T("Оновити статистику", "Refresh statistics")
        }, Theme.DayAccent, filled: false);
        refresh.Click += (_, _) => RefreshReport();
        var filters = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        filters.Controls.Add(Labeled(_date.AccessibleName, _date));
        filters.Controls.Add(Labeled(_period.AccessibleName, _period));
        filters.Controls.Add(refresh);

        var metrics = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        foreach (var kind in Enum.GetValues<UsageKind>())
        {
            var value = new Label
            {
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = new Font("Segoe UI Semibold", 17F),
                AccessibleName = UsageText.Kind(_language, kind)
            };
            _metrics.Add(kind, value);
            var metric = Labeled(UsageText.Kind(_language, kind), value);
            metric.MinimumSize = new Size(190, 68);
            metrics.Controls.Add(metric);
        }

        _applications.AccessibleName = T("Час у програмах", "Application time");
        AddColumn(_applications, "application", T("Програма", "Application"), 250);
        AddColumn(_applications, "minutes", T("Час, хв", "Time, min"), 110, numeric: true);
        AddColumn(_applications, "share", T("Частка активності, %", "Share of activity, %"), 160, numeric: true);

        _days.AccessibleName = T("Режим за днями", "Daily routine");
        AddColumn(_days, "date", T("Дата", "Date"), 110);
        _days.Columns["date"]!.ValueType = typeof(DateTime);
        AddColumn(_days, "recorded", T("Записано, хв", "Recorded, min"), 120, numeric: true);
        AddColumn(_days, "active", T("Активність, хв", "Activity, min"), 120, numeric: true);
        AddColumn(_days, "idle", T("Без вводу, хв", "No input, min"), 115, numeric: true);
        AddColumn(_days, "longest", T("Найдовша сесія, хв", "Longest session, min"), 150, numeric: true);
        AddColumn(_days, "night", T("У нічному вікні, хв", "Night activity, min"), 150, numeric: true);
        AddColumn(_days, "first", T("Перша активність", "First activity"), 145);
        AddColumn(_days, "last", T("Остання активність", "Last activity"), 145);

        _timeline.AccessibleName = T("Часова шкала вибраного дня", "Selected day timeline");
        AddColumn(_timeline, "start", T("Початок", "Start"), 135);
        AddColumn(_timeline, "end", T("Кінець", "End"), 135);
        AddColumn(_timeline, "kind", T("Стан", "State"), 190);
        AddColumn(_timeline, "app", T("Програма", "Application"), 190);
        AddColumn(_timeline, "minutes", T("Час, хв", "Time, min"), 100, numeric: true);

        var views = new TabControl { Dock = DockStyle.Fill, AccessibleName = T("Види статистики", "Statistics views") };
        AddView(views, T("Програми", "Applications"), _applications);
        AddView(views, T("За днями", "By day"), _days);
        AddView(views, T("Шкала вибраного дня", "Selected day timeline"), _timeline);

        var explanation = new Label
        {
            Text = string.Format(UiText.Culture(language), T(
                "Активність: ввід за останні {0} хв. Читання й відео без вводу можуть потрапляти в «Без вводу». Це не оцінка продуктивності чи відпочинку. Частка програми — від усієї активності. Дані локальні, зберігаються 90 днів.",
                "Activity: input within the last {0} min. Reading or video without input may appear as no input. This does not measure productivity or rest. Application shares use all activity. Data stays local for 90 days."), idleMinutes),
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 8, 0, 12)
        };
        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 6,
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = Theme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < 5; index++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(filters, 0, 0);
        layout.Controls.Add(_status, 0, 1);
        layout.Controls.Add(metrics, 0, 2);
        layout.Controls.Add(_coverage, 0, 3);
        layout.Controls.Add(explanation, 0, 4);
        layout.Controls.Add(views, 0, 5);
        Controls.Add(layout);
        _date.ValueChanged += (_, _) => RefreshReport();
        _period.SelectedIndexChanged += (_, _) => RefreshReport();
        _ready = true;
        RefreshReport();
    }

    internal void RefreshReport()
    {
        if (!_ready)
        {
            return;
        }

        _applications.Rows.Clear();
        _days.Rows.Clear();
        _timeline.Rows.Clear();
        try
        {
            _service.Flush();
            var zone = TimeZoneInfo.Local;
            _date.MaxDate = TimeZoneInfo.ConvertTime(_clock.UtcNow, zone).DateTime.Date;
            var selected = DateOnly.FromDateTime(_date.Value);
            var count = _period.SelectedIndex switch { 1 => 7, 2 => 30, _ => 1 };
            var start = UsageReport.LocalDayStart(selected.AddDays(1 - count), zone);
            var end = UsageReport.LocalDayStart(selected.AddDays(1), zone);
            var now = _clock.UtcNow;
            if (end > now)
            {
                end = now;
            }

            var source = _service.Store.Read(start, end);
            var report = UsageReport.Build(source, start, end, zone);
            foreach (var metric in _metrics)
            {
                metric.Value.Text = UsageText.Duration(_language, report.Total(metric.Key));
            }

            foreach (var app in report.Applications)
            {
                _applications.Rows.Add(UsageText.Application(_language, app.Application), app.Duration.TotalMinutes, app.Share * 100);
            }

            foreach (var day in report.Days.OrderByDescending(day => day.Date))
            {
                var known = day.Recorded > TimeSpan.Zero;
                _days.Rows.Add(day.Date.ToDateTime(TimeOnly.MinValue),
                    known ? day.Recorded.TotalMinutes : DBNull.Value,
                    known ? day.Active.TotalMinutes : DBNull.Value,
                    known ? day.Idle.TotalMinutes : DBNull.Value,
                    known ? day.LongestActive.TotalMinutes : DBNull.Value,
                    known ? day.NightActive.TotalMinutes : DBNull.Value,
                    LocalTime(day.FirstActive), LocalTime(day.LastActive));
            }

            _days.Columns["date"]!.DefaultCellStyle.Format = "d";
            var dayStart = UsageReport.LocalDayStart(selected, zone);
            var dayTimeline = UsageReport.Build(source, dayStart, end, zone).Timeline;
            foreach (var span in dayTimeline)
            {
                _timeline.Rows.Add(LocalTime(span.StartUtc), LocalTime(span.EndUtc),
                    UsageText.Kind(_language, span.Kind),
                    span.Kind == UsageKind.Active ? UsageText.Application(_language, span.Application) : "—",
                    span.Duration.TotalMinutes);
            }

            var recorded = report.Days.Aggregate(TimeSpan.Zero, (sum, day) => sum + day.Recorded);
            var night = report.Days.Aggregate(TimeSpan.Zero, (sum, day) => sum + day.NightActive);
            _coverage.Text = string.Format(UiText.Culture(_language), T(
                "{0:d} – {1:d} · Записано: {2} · Активність у нічному вікні: {3} · {4}",
                "{0:d} – {1:d} · Recorded: {2} · Night-window activity: {3} · {4}"),
                selected.AddDays(1 - count).ToDateTime(TimeOnly.MinValue), selected.ToDateTime(TimeOnly.MinValue),
                UsageText.Duration(_language, recorded), UsageText.Duration(_language, night),
                T("Локальний час Windows", "Windows local time"));
            var state = _service.SafeMode ? T("Безпечний режим: запис вимкнений", "Safe mode: recording disabled")
                : _service.Enabled ? T("Збір статистики триває", "Statistics recording is on")
                : T("Збір статистики призупинено", "Statistics recording is paused");
            _status.Text = state + " · " + T("Оновлено: ", "Updated: ") + LocalTime(now);
            if (recorded == TimeSpan.Zero)
            {
                _status.Text += T(" · За цей період записів немає. Минуле використання програм відновити неможливо.",
                    " · No recordings in this period. Past application usage cannot be reconstructed.");
            }

            if (_service.HasError)
            {
                ShowError();
            }
            else
            {
                _status.ForeColor = Theme.Muted;
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            foreach (var metric in _metrics.Values)
            {
                metric.Text = "—";
            }

            _coverage.Text = string.Empty;
            ShowError();
        }
    }

    private void ShowError()
    {
        _status.ForeColor = Theme.Danger;
        _status.Text = T("Статистика неповна: не вдалося прочитати або зберегти дані. Перевір доступ до локальної папки й натисни «Оновити». Захист перерв працює окремо.",
            "Statistics are incomplete: data could not be read or saved. Check access to the local folder and select Refresh. Break protection works independently.");
    }

    private string LocalTime(DateTimeOffset? time) => time is null ? "—"
        : TimeZoneInfo.ConvertTime(time.Value, TimeZoneInfo.Local).ToString("HH:mm:ss zzz", UiText.Culture(_language));

    private string T(string ukrainian, string english) => UsageText.Pick(_language, ukrainian, english);

    private static Control Labeled(string? text, Control control)
    {
        var host = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Margin = new Padding(0, 0, 16, 8) };
        host.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 4) });
        host.Controls.Add(control);
        return host;
    }

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Theme.Background,
        BorderStyle = BorderStyle.None,
        GridColor = Theme.Border,
        EnableHeadersVisualStyles = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            SelectionBackColor = Theme.Surface,
            WrapMode = DataGridViewTriState.True,
            Font = new Font("Segoe UI Semibold", 9F)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Background,
            ForeColor = Theme.Text,
            SelectionBackColor = Theme.Border,
            SelectionForeColor = Theme.Text,
            NullValue = "—",
            FormatProvider = System.Globalization.CultureInfo.CurrentCulture
        },
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        RowTemplate = { Height = 30 }
    };

    private void AddColumn(DataGridView grid, string name, string title, int width, bool numeric = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = title,
            MinimumWidth = width,
            FillWeight = width,
            SortMode = DataGridViewColumnSortMode.Automatic,
            ValueType = numeric ? typeof(double) : typeof(string)
        };
        column.DefaultCellStyle.FormatProvider = UiText.Culture(_language);
        if (numeric)
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.DefaultCellStyle.Format = "N1";
        }

        grid.Columns.Add(column);
    }

    private static void AddView(TabControl tabs, string title, Control content)
    {
        var page = new TabPage(title) { BackColor = Theme.Background, ForeColor = Theme.Text, Padding = new Padding(8) };
        page.Controls.Add(content);
        tabs.TabPages.Add(page);
    }
}
