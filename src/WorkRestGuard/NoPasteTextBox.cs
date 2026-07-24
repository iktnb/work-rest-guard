namespace WorkRestGuard;

internal sealed class NoPasteTextBox : TextBox
{
    internal NoPasteTextBox()
    {
        AllowDrop = false;
        Multiline = true;
        AcceptsReturn = true;
        AcceptsTab = false;
        ScrollBars = ScrollBars.Vertical;
        WordWrap = true;
        KeyDown += OnKeyDown;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg is NativeMethods.WmPaste or NativeMethods.WmContextMenu)
        {
            return;
        }

        base.WndProc(ref message);
    }

    private static bool IsPasteShortcut(KeyEventArgs eventArgs) =>
        (eventArgs.Control && eventArgs.KeyCode == Keys.V) ||
        (eventArgs.Shift && eventArgs.KeyCode == Keys.Insert);

    private static void Suppress(KeyEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (IsPasteShortcut(eventArgs))
        {
            Suppress(eventArgs);
        }
    }
}
