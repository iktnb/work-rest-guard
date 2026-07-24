namespace WorkRestGuard;

public interface IActivitySource
{
    bool PollHasNewInput();

    void ResetBaseline();
}

public sealed class InputActivitySource : IActivitySource
{
    private uint? _lastInputTime;

    public bool PollHasNewInput()
    {
        var current = ReadLastInputTime();
        if (current is null)
        {
            return false;
        }

        var changed = _lastInputTime is not null && _lastInputTime.Value != current.Value;
        _lastInputTime = current;
        return changed;
    }

    public void ResetBaseline()
    {
        _lastInputTime = ReadLastInputTime();
    }

    private static uint? ReadLastInputTime()
    {
        var info = NativeMethods.LastInputInfo.Create();
        return NativeMethods.GetLastInputInfo(ref info) ? info.Time : null;
    }
}
