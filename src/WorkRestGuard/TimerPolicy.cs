namespace WorkRestGuard;

public static class TimerPolicy
{
    public static bool IsQuietTime(DateTimeOffset now, GuardConfig config, TimeZoneInfo? timeZone = null)
    {
        var local = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone ?? TimeZoneInfo.Local).DateTime);
        return config.NightStart < config.NightEnd
            ? local >= config.NightStart && local < config.NightEnd
            : local >= config.NightStart || local < config.NightEnd;
    }

    public static bool ShouldShowTimer(DateTimeOffset now, GuardConfig config, GuardViewState view,
        TimeZoneInfo? timeZone = null) =>
        config.GuardEnabled && config.IsConfigured && !IsQuietTime(now, config, timeZone) &&
        view.DeadlineUtc is not null && view.Phase is GuardPhase.Working or GuardPhase.DayResting;
}
