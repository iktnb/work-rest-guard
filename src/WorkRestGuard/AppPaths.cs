namespace WorkRestGuard;

public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WorkRestGuard");

    public static string ConfigPath { get; } = Path.Combine(DataDirectory, "config.json");
    public static string StatePath { get; } = Path.Combine(DataDirectory, "state.json");
    public static string HistoryPath { get; } = Path.Combine(DataDirectory, "history.json");
    public static string LogPath { get; } = Path.Combine(DataDirectory, "logs", "guard.log");

    public static string UsageDirectory { get; } = Path.Combine(DataDirectory, "usage");
}
