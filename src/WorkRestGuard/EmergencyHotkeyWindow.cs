namespace WorkRestGuard;

internal sealed class EmergencyHotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 7311;
    private static readonly TimeSpan RequiredHold = TimeSpan.FromSeconds(5);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 100 };
    private DateTimeOffset? _holdStarted;
    private bool _disposed;

    internal EmergencyHotkeyWindow()
    {
        CreateHandle(new CreateParams());
        Registered = NativeMethods.RegisterHotKey(
            Handle,
            HotkeyId,
            NativeMethods.ModControl |
            NativeMethods.ModAlt |
            NativeMethods.ModShift |
            NativeMethods.ModNoRepeat,
            NativeMethods.VkF11);
        _timer.Tick += OnTimerTick;
    }

    internal event Action<TimeSpan?>? HoldProgressChanged;

    internal event Action? Completed;

    internal bool Registered { get; }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotkey &&
            message.WParam.ToInt32() == HotkeyId)
        {
            _holdStarted = DateTimeOffset.UtcNow;
            _timer.Start();
            HoldProgressChanged?.Invoke(RequiredHold);
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
        if (Registered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
        }

        DestroyHandle();
    }

    private void OnTimerTick(object? sender, EventArgs eventArgs)
    {
        if (_holdStarted is null || !IsChordDown())
        {
            CancelHold();
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - _holdStarted.Value;
        var remaining = RequiredHold - elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            _timer.Stop();
            _holdStarted = null;
            HoldProgressChanged?.Invoke(TimeSpan.Zero);
            Completed?.Invoke();
            return;
        }

        HoldProgressChanged?.Invoke(remaining);
    }

    private void CancelHold()
    {
        _timer.Stop();
        _holdStarted = null;
        HoldProgressChanged?.Invoke(null);
    }

    private static bool IsChordDown() =>
        IsKeyDown(NativeMethods.VkF11) &&
        IsKeyDown(NativeMethods.VkControl) &&
        IsKeyDown(NativeMethods.VkMenu) &&
        IsKeyDown(NativeMethods.VkShift);

    private static bool IsKeyDown(int virtualKey) =>
        (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
}
