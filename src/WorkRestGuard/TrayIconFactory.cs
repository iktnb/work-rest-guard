using System.Drawing.Drawing2D;

namespace WorkRestGuard;

internal static class TrayIconFactory
{
    internal static Icon Create(GuardPhase phase)
    {
        var accent = phase switch
        {
            GuardPhase.NightLocked or GuardPhase.NightGrace => Theme.NightAccent,
            GuardPhase.DayResting or GuardPhase.DaySnoozed => Theme.DayAccent,
            GuardPhase.Working => Theme.Success,
            _ => Theme.Muted
        };

        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            using var background = new SolidBrush(Theme.Background);
            using var accentBrush = new SolidBrush(accent);
            graphics.FillEllipse(background, 1, 1, 30, 30);
            graphics.FillEllipse(accentBrush, 6, 6, 20, 20);
            using var hand = new Pen(Theme.Text, 2.2F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            graphics.DrawLine(hand, 16, 16, 16, 9);
            graphics.DrawLine(hand, 16, 16, 21, 19);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
