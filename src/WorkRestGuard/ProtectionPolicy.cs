namespace WorkRestGuard;

public static class ProtectionPolicy
{
    private const int MinutesPerDay = 24 * 60;
    private const int MinutesPerWeek = 7 * MinutesPerDay;

    public static ChallengeKind RequiredChallenge(GuardConfig current, GuardConfig candidate)
    {
        if (!current.IsConfigured)
        {
            return ChallengeKind.None;
        }

        var monitorChanged =
            !string.Equals(
                current.ProtectedMonitor.HardwareIdHash,
                candidate.ProtectedMonitor.HardwareIdHash,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                current.ProtectedMonitor.DeviceId,
                candidate.ProtectedMonitor.DeviceId,
                StringComparison.OrdinalIgnoreCase);

        if (monitorChanged ||
            (current.LaunchAtSignIn && !candidate.LaunchAtSignIn) ||
            (current.GuardEnabled && !candidate.GuardEnabled) ||
            IsNightProtectionReduced(current, candidate))
        {
            return ChallengeKind.Night;
        }

        return candidate.WorkMinutes > current.WorkMinutes ||
               candidate.BreakMinutes < current.BreakMinutes
            ? ChallengeKind.Day
            : ChallengeKind.None;
    }

    public static bool IsNightProtectionReduced(GuardConfig current, GuardConfig candidate)
    {
        if (current.NightModeEnabled && !candidate.NightModeEnabled)
        {
            return true;
        }

        if (!current.NightModeEnabled)
        {
            return false;
        }

        var currentMinutes = BuildProtectedMinuteMap(current);
        var candidateMinutes = BuildProtectedMinuteMap(candidate);
        for (var minute = 0; minute < MinutesPerWeek; minute++)
        {
            if (currentMinutes[minute] && !candidateMinutes[minute])
            {
                return true;
            }
        }

        return false;
    }

    private static bool[] BuildProtectedMinuteMap(GuardConfig config)
    {
        var result = new bool[MinutesPerWeek];
        if (!config.NightModeEnabled || config.NightStart == config.NightEnd)
        {
            return result;
        }

        var startMinute = (config.NightStart.Hour * 60) + config.NightStart.Minute;
        var endMinute = (config.NightEnd.Hour * 60) + config.NightEnd.Minute;
        foreach (var startDay in config.NightDays.Distinct())
        {
            var dayIndex = (int)startDay;
            if (startMinute < endMinute)
            {
                Mark(result, dayIndex, startMinute, endMinute);
            }
            else
            {
                Mark(result, dayIndex, startMinute, MinutesPerDay);
                Mark(result, (dayIndex + 1) % 7, 0, endMinute);
            }
        }

        return result;
    }

    private static void Mark(bool[] result, int day, int fromMinute, int toMinute)
    {
        var offset = day * MinutesPerDay;
        for (var minute = fromMinute; minute < toMinute; minute++)
        {
            result[offset + minute] = true;
        }
    }
}
