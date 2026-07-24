namespace WorkRestGuard;

internal sealed class MonitorPreviewForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2200 };

    internal MonitorPreviewForm(
        MonitorDescriptor monitor,
        string languageCode)
    {
        Text = UiText.Get(languageCode, TextKey.PreviewMonitor);
        Name = nameof(MonitorPreviewForm);
        AccessibleName = Text;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = monitor.Bounds;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Theme.Background;
        Opacity = 0.97;
        KeyPreview = true;

        var accent = new Label
        {
            Text = "WORK REST GUARD",
            ForeColor = Theme.DayAccent,
            Font = new Font("Segoe UI Semibold", 24F),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var detail = new Label
        {
            Text = $"{UiText.Get(languageCode, TextKey.ProtectedMonitor)}\n" +
                   $"{monitor.FriendlyName} · {monitor.Bounds.Width}×{monitor.Bounds.Height}",
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 14F),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 18, 0, 0)
        };
        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            BackColor = Color.Transparent
        };
        stack.Controls.Add(accent);
        stack.Controls.Add(detail);

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        host.Controls.Add(stack, 0, 0);
        Controls.Add(host);

        _timer.Tick += (_, _) => Close();
        Shown += (_, _) => _timer.Start();
        KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
