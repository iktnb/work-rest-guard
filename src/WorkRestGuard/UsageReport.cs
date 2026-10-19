namespace WorkRestGuard;

internal sealed record ApplicationUsage(string? Application, TimeSpan Duration, double Share);

internal sealed record DailyUsage(
    DateOnly Date, TimeSpan Active, TimeSpan Idle, TimeSpan Recorded,
    TimeSpan LongestActive, TimeSpan NightActive,
    DateTimeOffset? FirstActive, DateTimeOffset? LastActive);

internal sealed class UsageReport
{
    internal required IReadOnlyList<UsageSpan> Timeline { get; init; }
    internal required IReadOnlyList<ApplicationUsage> Applications { get; init; }
    internal required IReadOnlyList<DailyUsage> Days { get; init; }
    internal TimeSpan Total(UsageKind kind) => TimeSpan.FromTicks(
        Timeline.Where(span => span.Kind == kind).Sum(span => span.Duration.Ticks));

    internal static UsageReport Build(
        IEnumerable<UsageSpan> source, DateTimeOffset startUtc, DateTimeOffset endUtc,
        TimeZoneInfo timeZone)
    {
        var timeline = new List<UsageSpan>();
        var cursor = startUtc;
        foreach (var span in source.OrderBy(item => item.StartUtc))
        {
            var start = span.StartUtc > cursor ? span.StartUtc : cursor;
            var end = span.EndUtc < endUtc ? span.EndUtc : endUtc;
            if (end <= start || start >= endUtc)
            {
                continue;
            }

            if (start > cursor)
            {
                timeline.Add(Gap(cursor, start));
            }

            timeline.Add(span with { StartUtc = start, EndUtc = end });
            cursor = end;
        }

        if (cursor < endUtc)
        {
            timeline.Add(Gap(cursor, endUtc));
        }

        var activeTicks = timeline.Where(span => span.Kind == UsageKind.Active)
            .Sum(span => span.Duration.Ticks);
        var applications = timeline.Where(span => span.Kind == UsageKind.Active)
            .GroupBy(span => span.Application, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ApplicationUsage(group.Key,
                TimeSpan.FromTicks(group.Sum(span => span.Duration.Ticks)),
                activeTicks == 0 ? 0 : (double)group.Sum(span => span.Duration.Ticks) / activeTicks))
            .OrderByDescending(item => item.Duration)
            .ThenBy(item => item.Application, StringComparer.OrdinalIgnoreCase).ToList();

        var days = new List<DailyUsage>();
        if (endUtc > startUtc)
        {
            var first = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(startUtc, timeZone).DateTime);
            var last = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(endUtc.AddTicks(-1), timeZone).DateTime);
            for (var day = first; day <= last; day = day.AddDays(1))
            {
                var start = LocalDayStart(day, timeZone);
                var end = LocalDayStart(day.AddDays(1), timeZone);
                var pieces = timeline.Where(span => span.StartUtc < end && span.EndUtc > start)
                    .Select(span => span with
                    {
                        StartUtc = span.StartUtc < start ? start : span.StartUtc,
                        EndUtc = span.EndUtc > end ? end : span.EndUtc
                    }).ToList();
                var active = pieces.Where(span => span.Kind == UsageKind.Active).ToList();
                long longest = 0, run = 0;
                foreach (var piece in pieces)
                {
                    run = piece.Kind == UsageKind.Active ? run + piece.Duration.Ticks : 0;
                    longest = Math.Max(longest, run);
                }

                days.Add(new DailyUsage(day,
                    TimeSpan.FromTicks(active.Sum(span => span.Duration.Ticks)),
                    TimeSpan.FromTicks(pieces.Where(span => span.Kind == UsageKind.Idle).Sum(span => span.Duration.Ticks)),
                    TimeSpan.FromTicks(pieces.Where(span => span.Kind != UsageKind.Unrecorded).Sum(span => span.Duration.Ticks)),
                    TimeSpan.FromTicks(longest),
                    TimeSpan.FromTicks(active.Where(span => span.IsNightWindow).Sum(span => span.Duration.Ticks)),
                    active.FirstOrDefault()?.StartUtc, active.LastOrDefault()?.EndUtc));
            }
        }

        return new UsageReport { Timeline = timeline, Applications = applications, Days = days };
    }

    internal static DateTimeOffset LocalDayStart(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        return zone.IsAmbiguousTime(local)
            ? new DateTimeOffset(local, zone.GetAmbiguousTimeOffsets(local).Max()).ToUniversalTime()
            : new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }

    private static UsageSpan Gap(DateTimeOffset start, DateTimeOffset end) =>
        new(start, end, UsageKind.Unrecorded, null, GuardPhase.Ready, false);
}
