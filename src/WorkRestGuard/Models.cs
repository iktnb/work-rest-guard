using System.Drawing;
using System.Text.Json.Serialization;

namespace WorkRestGuard;

public sealed class GuardConfig
{
    public int SchemaVersion { get; set; } = 1;
    public MonitorIdentity ProtectedMonitor { get; set; } = new();
    public int WorkMinutes { get; set; } = 90;
    public int BreakMinutes { get; set; } = 15;
    public bool NightModeEnabled { get; set; } = true;
    public TimeOnly NightStart { get; set; } = new(20, 0);
    public TimeOnly NightEnd { get; set; } = new(6, 0);
    public List<DayOfWeek> NightDays { get; set; } =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    public string LanguageCode { get; set; } = "uk";
    public bool LaunchAtSignIn { get; set; }
    public bool GuardEnabled { get; set; } = true;
    public bool UsageTrackingEnabled { get; set; } = true;
    public int UsageIdleMinutes { get; set; } = 5;

    [JsonIgnore]
    public bool IsConfigured => ProtectedMonitor.IsConfigured;

    public GuardConfig Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        ProtectedMonitor = ProtectedMonitor.Clone(),
        WorkMinutes = WorkMinutes,
        BreakMinutes = BreakMinutes,
        NightModeEnabled = NightModeEnabled,
        NightStart = NightStart,
        NightEnd = NightEnd,
        NightDays = [.. NightDays],
        LanguageCode = LanguageCode,
        LaunchAtSignIn = LaunchAtSignIn,
        GuardEnabled = GuardEnabled,
        UsageTrackingEnabled = UsageTrackingEnabled,
        UsageIdleMinutes = UsageIdleMinutes
    };

    public void Normalize()
    {
        ProtectedMonitor ??= new MonitorIdentity();
        NightDays ??= [];
        WorkMinutes = Math.Clamp(WorkMinutes, 15, 240);
        BreakMinutes = Math.Clamp(BreakMinutes, 5, 60);
        UsageIdleMinutes = Math.Clamp(UsageIdleMinutes, 1, 60);
        LanguageCode = string.Equals(LanguageCode, "en", StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "uk";
        NightDays = NightDays.Distinct().OrderBy(day => day).ToList();
    }
}

public sealed class MonitorIdentity
{
    public string DeviceId { get; set; } = string.Empty;
    public string HardwareIdHash { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }

    [JsonIgnore]
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(DeviceId) ||
        !string.IsNullOrWhiteSpace(HardwareIdHash);

    public static MonitorIdentity FromDescriptor(MonitorDescriptor descriptor) => new()
    {
        DeviceId = descriptor.DeviceId,
        HardwareIdHash = descriptor.HardwareIdHash,
        FriendlyName = descriptor.FriendlyName,
        Width = descriptor.Bounds.Width,
        Height = descriptor.Bounds.Height
    };

    public MonitorIdentity Clone() => new()
    {
        DeviceId = DeviceId,
        HardwareIdHash = HardwareIdHash,
        FriendlyName = FriendlyName,
        Width = Width,
        Height = Height
    };
}

public sealed record MonitorDescriptor(
    string DeviceName,
    string DeviceId,
    string HardwareIdHash,
    string FriendlyName,
    Rectangle Bounds,
    Rectangle WorkingArea,
    bool Primary)
{
    public string DisplayLabel =>
        $"{FriendlyName} — {Bounds.Width}×{Bounds.Height}" +
        (Primary ? " (Primary)" : string.Empty);
}

public enum GuardPhase
{
    Ready,
    Working,
    DaySnoozed,
    DayResting,
    NightLocked,
    NightGrace
}

public enum GuardEventType
{
    DayBreakCompleted,
    DaySnoozed,
    DayBreakSkipped,
    NightLocked,
    NightGraceUsed,
    NightCompleted,
    NightOverridden,
    NightInterrupted,
    EmergencyExit
}

public enum ChallengeKind
{
    None,
    Day,
    Night
}

public enum OverlayKind
{
    Day,
    Night
}

public sealed class GuardRuntimeState
{
    public int SchemaVersion { get; set; } = 1;
    public GuardPhase Phase { get; set; } = GuardPhase.Ready;
    public DateTimeOffset? WorkDeadlineUtc { get; set; }
    public DateTimeOffset? DayDeadlineUtc { get; set; }
    public bool DaySnoozeUsed { get; set; }
    public string? NightScheduleId { get; set; }
    public DateTimeOffset? NightEndUtc { get; set; }
    public DateTimeOffset? NightGraceDeadlineUtc { get; set; }
    public bool NightGraceUsed { get; set; }
    public string? NightOverrideScheduleId { get; set; }
    public DateTimeOffset LastHeartbeatUtc { get; set; }

    public GuardRuntimeState Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Phase = Phase,
        WorkDeadlineUtc = WorkDeadlineUtc,
        DayDeadlineUtc = DayDeadlineUtc,
        DaySnoozeUsed = DaySnoozeUsed,
        NightScheduleId = NightScheduleId,
        NightEndUtc = NightEndUtc,
        NightGraceDeadlineUtc = NightGraceDeadlineUtc,
        NightGraceUsed = NightGraceUsed,
        NightOverrideScheduleId = NightOverrideScheduleId,
        LastHeartbeatUtc = LastHeartbeatUtc
    };
}

public sealed record GuardEvent(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    GuardEventType Type,
    int? SnoozeMinutes = null,
    string? NightScheduleId = null,
    bool Verified = true);

public sealed record NightWindow(
    string Id,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record GuardViewState(
    GuardPhase Phase,
    bool ShowOverlay,
    OverlayKind? OverlayKind,
    DateTimeOffset? DeadlineUtc,
    bool DaySnoozeAvailable,
    bool NightGraceAvailable,
    string? NightScheduleId);

public sealed record HistoryDaySummary(
    DateOnly Date,
    int DayCompleted,
    int DaySnoozed,
    int DaySkipped,
    int NightCompleted,
    int NightGraceUsed,
    int NightOverridden,
    int NightInterrupted);
