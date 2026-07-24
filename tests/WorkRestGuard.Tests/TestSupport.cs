namespace WorkRestGuard.Tests;

internal sealed class FakeClock : IClock
{
    internal FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }

    internal void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}

internal static class TestData
{
    internal static GuardConfig Config() => new()
    {
        ProtectedMonitor = new MonitorIdentity
        {
            DeviceId = "MONITOR\\WORK\\1",
            HardwareIdHash = "WORK-HASH",
            FriendlyName = "Work monitor",
            Width = 2560,
            Height = 1440
        },
        WorkMinutes = 90,
        BreakMinutes = 15,
        NightModeEnabled = true,
        NightStart = new TimeOnly(20, 0),
        NightEnd = new TimeOnly(6, 0),
        NightDays =
        [
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        ],
        GuardEnabled = true
    };
}
