namespace WorkRestGuard;

internal sealed class ChallengeForm : Form
{
    internal ChallengeForm(ChallengeKind kind, string languageCode)
    {
        var accent = kind == ChallengeKind.Night ? Theme.NightAccent : Theme.DayAccent;
        var statement = kind == ChallengeKind.Night
            ? UiText.NightStatement(languageCode)
            : UiText.DayStatement(languageCode);

        Text = UiText.Get(languageCode, TextKey.ChallengeRequired);
        Name = nameof(ChallengeForm);
        AccessibleName = Text;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 620);
        Size = new Size(940, 760);
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new ExactTypingPanel(
            statement,
            languageCode,
            Theme.Background,
            accent,
            showBackButton: true)
        {
            Dock = DockStyle.Top,
            Margin = new Padding(0)
        };
        panel.Completed += () =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };
        panel.Cancelled += () =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(28),
            BackColor = Theme.Background
        };
        scroll.Controls.Add(panel);
        Controls.Add(scroll);

        Shown += (_, _) => panel.FocusInput();
    }
}
