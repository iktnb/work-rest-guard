namespace WorkRestGuard;

internal enum UsageKind
{
    Active,
    Idle,
    Locked,
    Suspended,
    Unknown,
    Unrecorded
}

internal sealed record UsageSpan(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    UsageKind Kind,
    string? Application,
    GuardPhase Phase,
    bool IsNightWindow)
{
    internal TimeSpan Duration => EndUtc - StartUtc;
}

internal sealed record UsageObservation(
    DateTimeOffset AtUtc,
    long TickMilliseconds,
    UsageKind Kind,
    string? Application,
    GuardPhase Phase,
    bool IsNightWindow);

internal sealed class UsageTracker
{
    private UsageObservation? _previous;

    internal UsageSpan? Observe(UsageObservation current)
    {
        var previous = _previous;
        _previous = current;
        if (previous is null)
        {
            return null;
        }

        var wall = current.AtUtc - previous.AtUtc;
        var elapsed = current.TickMilliseconds - previous.TickMilliseconds;
        if (elapsed <= 0 || wall <= TimeSpan.Zero ||
            Math.Abs(wall.TotalMilliseconds - elapsed) > 2000 ||
            (elapsed > 15000 && previous.Kind != UsageKind.Suspended))
        {
            return null;
        }

        return new UsageSpan(
            previous.AtUtc, current.AtUtc, previous.Kind,
            previous.Kind == UsageKind.Active ? previous.Application : null,
            previous.Phase, previous.IsNightWindow);
    }

    internal void Reset() => _previous = null;
}
