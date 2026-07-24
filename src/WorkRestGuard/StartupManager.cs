using Microsoft.Win32;

namespace WorkRestGuard;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (enabled)
        {
            var executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("The executable path is unavailable.");
            key.SetValue(
                AppIdentity.StartupValueName,
                $"\"{executablePath}\" --background",
                RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(AppIdentity.StartupValueName, false);
        }
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(AppIdentity.StartupValueName) is string value &&
               !string.IsNullOrWhiteSpace(value);
    }
}
