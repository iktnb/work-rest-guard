using System.ComponentModel;
using System.Diagnostics;

namespace WorkRestGuard;

internal sealed record UsageSnapshot(UsageKind Kind, string? Application);

internal interface IUsageSource
{
    UsageSnapshot Capture(int idleMinutes);
}

internal sealed class WindowsUsageSource : IUsageSource
{
    public UsageSnapshot Capture(int idleMinutes)
    {
        var input = NativeMethods.LastInputInfo.Create();
        if (!NativeMethods.GetLastInputInfo(ref input))
        {
            return new UsageSnapshot(UsageKind.Unknown, null);
        }

        var window = NativeMethods.GetForegroundWindow();
        if (window == 0)
        {
            return new UsageSnapshot(UsageKind.Unknown, null);
        }

        var kind = ClassifyInput(unchecked((uint)Environment.TickCount), input.Time, idleMinutes);
        if (kind != UsageKind.Active)
        {
            return new UsageSnapshot(kind, null);
        }

        if (NativeMethods.GetWindowThreadProcessId(window, out var processId) == 0 || processId == 0)
        {
            return new UsageSnapshot(UsageKind.Active, null);
        }

        try
        {
            using var process = Process.GetProcessById(checked((int)processId));
            return new UsageSnapshot(UsageKind.Active, process.ProcessName.ToLowerInvariant() + ".exe");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or Win32Exception or OverflowException)
        {
            return new UsageSnapshot(UsageKind.Active, null);
        }
    }

    internal static UsageKind ClassifyInput(uint currentTick, uint lastInputTick, int idleMinutes)
    {
        // Unsigned subtraction handles the counters' 49-day wraparound. A
        // future/non-monotonic input timestamp is unknown rather than days idle.
        var elapsed = unchecked(currentTick - lastInputTick);
        return elapsed > int.MaxValue ? UsageKind.Unknown
            : elapsed >= (long)idleMinutes * 60 * 1000 ? UsageKind.Idle : UsageKind.Active;
    }
}
