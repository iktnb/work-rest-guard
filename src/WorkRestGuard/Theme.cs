namespace WorkRestGuard;

internal static class Theme
{
    internal static readonly Color Background = Color.FromArgb(15, 23, 32);
    internal static readonly Color NightBackground = Color.FromArgb(10, 19, 35);
    internal static readonly Color Surface = Color.FromArgb(25, 35, 45);
    internal static readonly Color Text = Color.FromArgb(244, 247, 249);
    internal static readonly Color Muted = Color.FromArgb(184, 195, 204);
    internal static readonly Color Border = Color.FromArgb(66, 82, 94);
    internal static readonly Color DayAccent = Color.FromArgb(229, 169, 59);
    internal static readonly Color NightAccent = Color.FromArgb(94, 151, 226);
    internal static readonly Color Success = Color.FromArgb(74, 181, 132);
    internal static readonly Color Danger = Color.FromArgb(224, 102, 102);
    internal static readonly Color Focus = Color.FromArgb(247, 207, 112);

    internal static Button StyleButton(Button button, Color accent, bool filled)
    {
        button.AutoSize = true;
        button.MinimumSize = new Size(160, 46);
        button.Padding = new Padding(14, 7, 14, 7);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = filled ? accent : Border;
        button.BackColor = filled ? accent : Surface;
        button.ForeColor = filled ? Background : Text;
        button.Font = new Font("Segoe UI Semibold", 10F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        return button;
    }
}
