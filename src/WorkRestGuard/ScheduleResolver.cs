using System.Globalization;

namespace WorkRestGuard;

public interface IScheduleResolver
{
    NightWindow? GetActiveNightWindow(DateTimeOffset utcNow, GuardConfig config);
}

public sealed class ScheduleResolver : IScheduleResolver
{
    private readonly TimeZoneInfo _timeZone;

    public ScheduleResolver(TimeZoneInfo? timeZone = null)
    {
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public NightWindow? GetActiveNightWindow(DateTimeOffset utcNow, GuardConfig config)
    {
        if (!config.NightModeEnabled || config.NightDays.Count == 0)
        {
            return null;
        }

        var local = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var localDate = DateOnly.FromDateTime(local.DateTime);
        var localTime = TimeOnly.FromDateTime(local.DateTime);
        var crossesMidnight = config.NightStart >= config.NightEnd;

        DateOnly startDate;
        if (crossesMidnight)
        {
            if (localTime >= config.NightStart)
            {
                startDate = localDate;
            }
            else if (localTime < config.NightEnd)
            {
                startDate = localDate.AddDays(-1);
            }
            else
            {
                return null;
            }
        }
        else
        {
            if (localTime < config.NightStart || localTime >= config.NightEnd)
            {
                return null;
            }

            startDate = localDate;
        }

        if (!config.NightDays.Contains(startDate.DayOfWeek))
        {
            return null;
        }

        var endDate = crossesMidnight ? startDate.AddDays(1) : startDate;
        var startsAtUtc = ConvertLocalToUtc(startDate, config.NightStart, preferLaterOffset: false);
        var endsAtUtc = ConvertLocalToUtc(endDate, config.NightEnd, preferLaterOffset: true);
        if (utcNow < startsAtUtc || utcNow >= endsAtUtc)
        {
            return null;
        }

        return new NightWindow(
            startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            startsAtUtc,
            endsAtUtc);
    }

    private DateTimeOffset ConvertLocalToUtc(
        DateOnly date,
        TimeOnly time,
        bool preferLaterOffset)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        while (_timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        if (_timeZone.IsAmbiguousTime(local))
        {
            var offsets = _timeZone.GetAmbiguousTimeOffsets(local);
            var offset = preferLaterOffset ? offsets.Min() : offsets.Max();
            return new DateTimeOffset(local, offset).ToUniversalTime();
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }
}
