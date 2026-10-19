namespace WorkRestGuard;

internal static class AppIdentity
{
    internal const string ProductName = "Work Rest Guard";
    internal const string MutexName = @"Local\WorkRestGuard.SingleInstance";
    internal const string ShowSettingsEventName = @"Local\WorkRestGuard.ShowSettings";
    internal const string UpdateStopEventName = @"Local\WorkRestGuard.StopForUpdate";
    internal const string StartupValueName = "WorkRestGuard";
}
