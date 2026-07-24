namespace WorkRestGuard;

public static class CountdownFormatter
{
    public static string FormatRemaining(
        DateTimeOffset? deadlineUtc,
        DateTimeOffset nowUtc)
    {
        if (deadlineUtc is null)
        {
            return "--:--";
        }

        var totalSeconds = Math.Max(
            0,
            (long)Math.Ceiling((deadlineUtc.Value - nowUtc).TotalSeconds));
        var hours = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        return hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }
}
