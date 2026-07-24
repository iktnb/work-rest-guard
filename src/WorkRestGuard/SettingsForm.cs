namespace WorkRestGuard;

internal sealed class SettingsForm : Form
{
    private readonly GuardConfig _current;
    private readonly IReadOnlyList<MonitorDescriptor> _monitors;
    private readonly MonitorCatalog _monitorCatalog;
    private readonly HistoryStore _historyStore;
    private readonly IClock _clock;
    private readonly string _languageCode;
    private readonly ComboBox _monitor = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };
    private readonly NumericUpDown _workMinutes = new()
    {
        Minimum = 15,
        Maximum = 240,
        Dock = DockStyle.Fill
    };
    private readonly NumericUpDown _breakMinutes = new()
    {
        Minimum = 5,
        Maximum = 60,
        Dock = DockStyle.Fill
    };
    private readonly CheckBox _nightMode = new() { AutoSize = true };
    private readonly DateTimePicker _nightStart = CreateTimePicker();
    private readonly DateTimePicker _nightEnd = CreateTimePicker();
    private readonly CheckedListBox _nightDays = new()
    {
        CheckOnClick = true,
        Height = 116,
        Dock = DockStyle.Fill
    };
    private readonly ComboBox _language = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };
    private readonly CheckBox _startup = new() { AutoSize = true };
    private readonly CheckBox _enabled = new() { AutoSize = true };
    private readonly DataGridView _historyGrid = new();
    private readonly ListView _eventList = new();
    private readonly Label _historyEmpty = new() { AutoSize = true };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    internal SettingsForm(
        GuardConfig current,
        IReadOnlyList<MonitorDescriptor> monitors,
        MonitorCatalog monitorCatalog,
        HistoryStore historyStore,
        IClock clock,
        bool openHistory)
    {
        _current = current.Clone();
        _monitors = monitors;
        _monitorCatalog = monitorCatalog;
        _historyStore = historyStore;
        _clock = clock;
        _languageCode = current.LanguageCode;
        ResultConfig = current.Clone();

        Text = UiText.Get(_languageCode, TextKey.SettingsTitle);
        Name = nameof(SettingsForm);
        AccessibleName = Text;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 680);
        Size = new Size(980, 820);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        MaximizeBox = false;

        PopulateInputs();

        var settingsPage = new TabPage(UiText.Get(_languageCode, TextKey.SettingsTab))
        {
            BackColor = Theme.Background,
            ForeColor = Theme.Text,
            Padding = new Padding(0)
        };
        settingsPage.Controls.Add(BuildSettingsPage());

        var historyPage = new TabPage(UiText.Get(_languageCode, TextKey.HistoryTab))
        {
            BackColor = Theme.Background,
            ForeColor = Theme.Text,
            Padding = new Padding(0)
        };
        historyPage.Controls.Add(BuildHistoryPage());

        _tabs.TabPages.Add(settingsPage);
        _tabs.TabPages.Add(historyPage);
        _tabs.SelectedIndex = openHistory ? 1 : 0;
        Controls.Add(_tabs);
    }

    internal GuardConfig ResultConfig { get; private set; }

    internal bool StartBreakRequested { get; private set; }

    private static DateTimePicker CreateTimePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        Dock = DockStyle.Fill
    };

    private void PopulateInputs()
    {
        foreach (var monitor in _monitors)
        {
            _monitor.Items.Add(monitor);
        }

        _monitor.DisplayMember = nameof(MonitorDescriptor.DisplayLabel);
        _monitor.SelectedItem =
            _monitorCatalog.Find(_monitors, _current.ProtectedMonitor) ??
            MonitorCatalog.ChooseDefault(_monitors);

        _workMinutes.Value = Math.Clamp(_current.WorkMinutes, 15, 240);
        _breakMinutes.Value = Math.Clamp(_current.BreakMinutes, 5, 60);
        _nightMode.Checked = _current.NightModeEnabled;
        _nightMode.Text = UiText.Get(_languageCode, TextKey.NightMode);
        _nightStart.Value = DateTime.Today.Add(_current.NightStart.ToTimeSpan());
        _nightEnd.Value = DateTime.Today.Add(_current.NightEnd.ToTimeSpan());

        var culture = UiText.Culture(_languageCode);
        var orderedDays = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };
        foreach (var day in orderedDays)
        {
            var item = new DayOption(
                day,
                culture.DateTimeFormat.GetDayName(day));
            _nightDays.Items.Add(item, _current.NightDays.Contains(day));
        }

        _language.Items.Add(new LanguageOption("uk", "Українська"));
        _language.Items.Add(new LanguageOption("en", "English"));
        _language.SelectedIndex = UiText.IsEnglish(_current.LanguageCode) ? 1 : 0;
        _startup.Checked = _current.LaunchAtSignIn;
        _startup.Text = UiText.Get(_languageCode, TextKey.StartWithWindows);
        _enabled.Checked = _current.GuardEnabled;
        _enabled.Text = UiText.Get(_languageCode, TextKey.GuardEnabled);
    }

    private Control BuildSettingsPage()
    {
        var heading = new Label
        {
            Text = UiText.Get(_languageCode, TextKey.SetupHeading),
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI Semibold", 19F),
            AutoSize = true,
            MaximumSize = new Size(850, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        var description = new Label
        {
            Text = UiText.Get(_languageCode, TextKey.SetupDescription),
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 10.5F),
            AutoSize = true,
            MaximumSize = new Size(850, 0),
            Margin = new Padding(0, 0, 0, 18)
        };

        var preview = Theme.StyleButton(
            new Button
            {
                Text = UiText.Get(_languageCode, TextKey.PreviewMonitor),
                AccessibleName = UiText.Get(_languageCode, TextKey.PreviewMonitor)
            },
            Theme.DayAccent,
            filled: false);
        preview.Click += (_, _) => PreviewMonitor();

        var monitorRow = CreateTwoColumnRow(_monitor, preview);
        var timeRow = CreateTwoColumnRow(
            CreateLabeledControl(
                UiText.Get(_languageCode, TextKey.NightStart),
                _nightStart),
            CreateLabeledControl(
                UiText.Get(_languageCode, TextKey.NightEnd),
                _nightEnd));

        var save = Theme.StyleButton(
            new Button
            {
                Text = UiText.Get(_languageCode, TextKey.SaveApply),
                AccessibleName = UiText.Get(_languageCode, TextKey.SaveApply)
            },
            Theme.DayAccent,
            filled: true);
        save.Click += (_, _) => SaveAndClose(startBreak: false);

        var cancel = Theme.StyleButton(
            new Button
            {
                Text = UiText.Get(_languageCode, TextKey.Cancel),
                DialogResult = DialogResult.Cancel,
                AccessibleName = UiText.Get(_languageCode, TextKey.Cancel)
            },
            Theme.DayAccent,
            filled: false);

        var startBreak = Theme.StyleButton(
            new Button
            {
                Text = UiText.Get(_languageCode, TextKey.StartBreakNow),
                AccessibleName = UiText.Get(_languageCode, TextKey.StartBreakNow)
            },
            Theme.Success,
            filled: false);
        startBreak.Click += (_, _) => SaveAndClose(startBreak: true);

        var actions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 18, 0, 0)
        };
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        actions.Controls.Add(startBreak);

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Padding = new Padding(30),
            BackColor = Theme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(heading);
        layout.Controls.Add(description);
        layout.Controls.Add(CreateLabel(UiText.Get(_languageCode, TextKey.ProtectedMonitor)));
        layout.Controls.Add(monitorRow);
        layout.Controls.Add(CreateLabel(UiText.Get(_languageCode, TextKey.WorkMinutes)));
        layout.Controls.Add(_workMinutes);
        layout.Controls.Add(CreateLabel(UiText.Get(_languageCode, TextKey.BreakMinutes)));
        layout.Controls.Add(_breakMinutes);
        layout.Controls.Add(_nightMode);
        layout.Controls.Add(timeRow);
        layout.Controls.Add(CreateLabel(UiText.Get(_languageCode, TextKey.NightDays)));
        layout.Controls.Add(_nightDays);
        layout.Controls.Add(CreateLabel(UiText.Get(_languageCode, TextKey.Language)));
        layout.Controls.Add(_language);
        layout.Controls.Add(_startup);
        layout.Controls.Add(_enabled);
        layout.Controls.Add(actions);

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Background
        };
        scroll.Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
        return scroll;
    }

    private Control BuildHistoryPage()
    {
        var description = new Label
        {
            Text = UiText.Get(_languageCode, TextKey.HistoryDescription),
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 10.5F),
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(860, 0),
            Margin = new Padding(0, 0, 0, 14)
        };

        ConfigureHistoryGrid();
        ConfigureEventList();
        LoadHistory();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Size = new Size(800, 600),
            SplitterDistance = 340,
            BackColor = Theme.Border,
            Panel1MinSize = 220,
            Panel2MinSize = 160
        };
        split.Panel1.Controls.Add(_historyGrid);

        var detailHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background
        };
        _historyEmpty.Text = UiText.Get(_languageCode, TextKey.HistoryEmpty);
        _historyEmpty.ForeColor = Theme.Muted;
        _historyEmpty.Padding = new Padding(8);
        _historyEmpty.Dock = DockStyle.Top;
        detailHost.Controls.Add(_eventList);
        detailHost.Controls.Add(_historyEmpty);
        split.Panel2.Controls.Add(detailHost);

        var host = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            BackColor = Theme.Background
        };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(description, 0, 0);
        host.Controls.Add(split, 0, 1);
        return host;
    }

    private void ConfigureHistoryGrid()
    {
        _historyGrid.Dock = DockStyle.Fill;
        _historyGrid.ReadOnly = true;
        _historyGrid.AllowUserToAddRows = false;
        _historyGrid.AllowUserToDeleteRows = false;
        _historyGrid.AllowUserToResizeRows = false;
        _historyGrid.MultiSelect = false;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.RowHeadersVisible = false;
        _historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _historyGrid.BackgroundColor = Theme.Background;
        _historyGrid.BorderStyle = BorderStyle.None;
        _historyGrid.GridColor = Theme.Border;
        _historyGrid.EnableHeadersVisualStyles = false;
        _historyGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            SelectionBackColor = Theme.Surface,
            Font = new Font("Segoe UI Semibold", 9F)
        };
        _historyGrid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Background,
            ForeColor = Theme.Text,
            SelectionBackColor = Color.FromArgb(43, 64, 80),
            SelectionForeColor = Theme.Text,
            Font = new Font("Segoe UI", 9F)
        };
        _historyGrid.AccessibleName = UiText.Get(_languageCode, TextKey.HistoryTab);
        _historyGrid.SelectionChanged += (_, _) => RefreshSelectedEvents();

        AddHistoryColumn("date", TextKey.Date, 130);
        AddHistoryColumn("dayCompleted", TextKey.DayCompleted, 105);
        AddHistoryColumn("daySnoozed", TextKey.DaySnoozed, 105);
        AddHistoryColumn("daySkipped", TextKey.DaySkipped, 105);
        AddHistoryColumn("nightCompleted", TextKey.NightCompleted, 105);
        AddHistoryColumn("nightGrace", TextKey.NightGrace, 95);
        AddHistoryColumn("nightOverridden", TextKey.NightOverridden, 105);
        AddHistoryColumn("nightInterrupted", TextKey.NightInterrupted, 105);
    }

    private void AddHistoryColumn(string name, TextKey key, float fillWeight)
    {
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = UiText.Get(_languageCode, key),
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
    }

    private void ConfigureEventList()
    {
        _eventList.Dock = DockStyle.Fill;
        _eventList.View = View.Details;
        _eventList.FullRowSelect = true;
        _eventList.HideSelection = false;
        _eventList.BackColor = Theme.Background;
        _eventList.ForeColor = Theme.Text;
        _eventList.BorderStyle = BorderStyle.None;
        _eventList.AccessibleName = UiText.Get(_languageCode, TextKey.EventType);
        _eventList.Columns.Add(UiText.Get(_languageCode, TextKey.EventTime), 130);
        _eventList.Columns.Add(UiText.Get(_languageCode, TextKey.EventType), 560);
    }

    private void LoadHistory()
    {
        var summaries = _historyStore.GetDailySummaries(_clock.UtcNow);
        foreach (var summary in summaries)
        {
            var rowIndex = _historyGrid.Rows.Add(
                summary.Date.ToString("d", UiText.Culture(_languageCode)),
                summary.DayCompleted,
                summary.DaySnoozed,
                summary.DaySkipped,
                summary.NightCompleted,
                summary.NightGraceUsed,
                summary.NightOverridden,
                summary.NightInterrupted);
            _historyGrid.Rows[rowIndex].Tag = summary.Date;
        }

        if (_historyGrid.Rows.Count > 0)
        {
            _historyGrid.Rows[0].Selected = true;
        }
    }

    private void RefreshSelectedEvents()
    {
        if (_historyGrid.SelectedRows.Count == 0 ||
            _historyGrid.SelectedRows[0].Tag is not DateOnly selectedDate)
        {
            return;
        }

        _eventList.Items.Clear();
        var events = _historyStore.GetEvents(_clock.UtcNow)
            .Where(item =>
                DateOnly.FromDateTime(item.OccurredAtUtc.ToLocalTime().DateTime) == selectedDate)
            .OrderByDescending(item => item.OccurredAtUtc);

        foreach (var guardEvent in events)
        {
            var local = guardEvent.OccurredAtUtc.ToLocalTime();
            var detail = FormatEvent(guardEvent);
            var item = new ListViewItem(local.ToString("T", UiText.Culture(_languageCode)));
            item.SubItems.Add(detail);
            _eventList.Items.Add(item);
        }

        _historyEmpty.Visible = _eventList.Items.Count == 0;
    }

    private string FormatEvent(GuardEvent guardEvent)
    {
        var localized = guardEvent.Type switch
        {
            GuardEventType.DayBreakCompleted => UiText.Get(_languageCode, TextKey.DayCompleted),
            GuardEventType.DaySnoozed => UiText.Get(_languageCode, TextKey.DaySnoozed),
            GuardEventType.DayBreakSkipped => UiText.Get(_languageCode, TextKey.DaySkipped),
            GuardEventType.NightCompleted => UiText.Get(_languageCode, TextKey.NightCompleted),
            GuardEventType.NightGraceUsed => UiText.Get(_languageCode, TextKey.NightGrace),
            GuardEventType.NightOverridden => UiText.Get(_languageCode, TextKey.NightOverridden),
            GuardEventType.NightInterrupted => UiText.Get(_languageCode, TextKey.NightInterrupted),
            GuardEventType.NightLocked => UiText.Get(_languageCode, TextKey.StatusNightLocked),
            GuardEventType.EmergencyExit => UiText.Get(_languageCode, TextKey.EmergencyExit),
            _ => guardEvent.Type.ToString()
        };

        if (guardEvent.SnoozeMinutes is not null)
        {
            localized += $" · {guardEvent.SnoozeMinutes} min";
        }

        return guardEvent.Verified ? localized : $"{localized} · unverified";
    }

    private void PreviewMonitor()
    {
        if (_monitor.SelectedItem is not MonitorDescriptor selected)
        {
            return;
        }

        using var preview = new MonitorPreviewForm(selected, _languageCode);
        preview.ShowDialog(this);
    }

    private void SaveAndClose(bool startBreak)
    {
        if (_monitor.SelectedItem is not MonitorDescriptor selectedMonitor)
        {
            MessageBox.Show(
                this,
                UiText.Get(_languageCode, TextKey.MonitorMissing),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var start = TimeOnly.FromDateTime(_nightStart.Value);
        var end = TimeOnly.FromDateTime(_nightEnd.Value);
        if (_nightMode.Checked && start == end)
        {
            MessageBox.Show(
                this,
                UiText.Get(_languageCode, TextKey.InvalidSchedule),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var candidate = new GuardConfig
        {
            SchemaVersion = 1,
            ProtectedMonitor = MonitorIdentity.FromDescriptor(selectedMonitor),
            WorkMinutes = (int)_workMinutes.Value,
            BreakMinutes = (int)_breakMinutes.Value,
            NightModeEnabled = _nightMode.Checked,
            NightStart = start,
            NightEnd = end,
            NightDays = _nightDays.CheckedItems
                .OfType<DayOption>()
                .Select(option => option.Day)
                .ToList(),
            LanguageCode = (_language.SelectedItem as LanguageOption)?.Code ?? "uk",
            LaunchAtSignIn = _startup.Checked,
            GuardEnabled = _enabled.Checked
        };
        candidate.Normalize();

        var challenge = ProtectionPolicy.RequiredChallenge(_current, candidate);
        if (challenge != ChallengeKind.None)
        {
            using var challengeForm = new ChallengeForm(challenge, _languageCode);
            if (challengeForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
        }

        ResultConfig = candidate;
        StartBreakRequested = startBreak;
        DialogResult = DialogResult.OK;
        Close();
    }

    private Label CreateLabel(string text) => new()
    {
        Text = text,
        ForeColor = Theme.Text,
        Font = new Font("Segoe UI Semibold", 9.5F),
        AutoSize = true,
        Margin = new Padding(0, 12, 0, 5)
    };

    private Control CreateLabeledControl(string label, Control control)
    {
        var host = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        host.Controls.Add(CreateLabel(label));
        host.Controls.Add(control);
        return host;
    }

    private static TableLayoutPanel CreateTwoColumnRow(Control left, Control right)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        left.Margin = new Padding(0, 0, 8, 0);
        right.Margin = new Padding(8, 0, 0, 0);
        row.Controls.Add(left, 0, 0);
        row.Controls.Add(right, 1, 0);
        return row;
    }

    private sealed record DayOption(DayOfWeek Day, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed record LanguageOption(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
