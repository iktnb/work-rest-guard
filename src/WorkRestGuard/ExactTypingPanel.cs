namespace WorkRestGuard;

internal sealed class ExactTypingPanel : UserControl
{
    private readonly string _expected;
    private readonly string _languageCode;
    private readonly NoPasteTextBox _input;
    private readonly Label _feedback;
    private readonly Button _confirm;

    internal ExactTypingPanel(
        string expected,
        string languageCode,
        Color background,
        Color accent,
        bool showBackButton)
    {
        _expected = expected;
        _languageCode = languageCode;

        AutoSize = false;
        Height = 600;
        MinimumSize = new Size(620, 600);
        BackColor = background;
        AccessibleName = UiText.Get(languageCode, TextKey.ExactInstruction);

        var instruction = new Label
        {
            Text = UiText.Get(languageCode, TextKey.ExactInstruction),
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 10.5F),
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Margin = new Padding(0, 0, 0, 12)
        };

        var reference = new Label
        {
            Text = expected,
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
            Font = new Font("Segoe UI", 10.5F),
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 12),
            AccessibleName = UiText.Get(languageCode, TextKey.ExactInstruction),
            AccessibleDescription = expected
        };

        _input = new NoPasteTextBox
        {
            Dock = DockStyle.Fill,
            Height = 148,
            MinimumSize = new Size(620, 148),
            BackColor = Color.FromArgb(10, 17, 24),
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10.5F),
            AccessibleName = UiText.Get(languageCode, TextKey.ExactInstruction),
            Margin = new Padding(0, 0, 0, 8)
        };
        _input.TextChanged += (_, _) => RefreshValidation();

        _feedback = new Label
        {
            Text = string.Format(
                UiText.Culture(languageCode),
                UiText.Get(languageCode, TextKey.ExactProgress),
                0,
                expected.Length),
            ForeColor = Theme.Muted,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 0, 0, 14),
            AccessibleName = "Typing progress"
        };

        _confirm = Theme.StyleButton(
            new Button
            {
                Text = UiText.Get(languageCode, TextKey.Confirm),
                Enabled = false,
                AccessibleName = UiText.Get(languageCode, TextKey.Confirm)
            },
            accent,
            filled: true);
        _confirm.Click += (_, _) =>
        {
            if (ExactTypingMatcher.IsExactMatch(_expected, _input.Text))
            {
                Completed?.Invoke();
            }
        };

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        actions.Controls.Add(_confirm);
        if (showBackButton)
        {
            var back = Theme.StyleButton(
                new Button
                {
                    Text = UiText.Get(languageCode, TextKey.Back),
                    AccessibleName = UiText.Get(languageCode, TextKey.Back)
                },
                accent,
                filled: false);
            back.Click += (_, _) => Cancelled?.Invoke();
            actions.Controls.Add(back);
        }

        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(instruction);
        layout.Controls.Add(reference);
        layout.Controls.Add(_input);
        layout.Controls.Add(_feedback);
        layout.Controls.Add(actions);
        Controls.Add(layout);
    }

    internal event Action? Completed;

    internal event Action? Cancelled;

    internal void FocusInput() => _input.Focus();

    internal void ClearInput()
    {
        _input.Clear();
        RefreshValidation();
    }

    private void RefreshValidation()
    {
        var mismatch = ExactTypingMatcher.FindFirstMismatch(_expected, _input.Text);
        var isExact = ExactTypingMatcher.IsExactMatch(_expected, _input.Text);
        _confirm.Enabled = isExact;

        if (mismatch is not null)
        {
            _feedback.Text = string.Format(
                UiText.Culture(_languageCode),
                UiText.Get(_languageCode, TextKey.ExactMismatch),
                mismatch.Value + 1);
            _feedback.ForeColor = Theme.Danger;
            return;
        }

        _feedback.Text = string.Format(
            UiText.Culture(_languageCode),
            UiText.Get(_languageCode, TextKey.ExactProgress),
            Math.Min(_input.Text.Length, _expected.Length),
            _expected.Length);
        _feedback.ForeColor = isExact ? Theme.Success : Theme.Muted;
    }
}
