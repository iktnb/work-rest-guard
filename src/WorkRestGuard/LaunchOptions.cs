namespace WorkRestGuard;

internal sealed record LaunchOptions(
    bool Background,
    bool SafeMode,
    bool OpenHistory,
    bool OpenStatistics)
{
    internal static LaunchOptions Parse(string[] args)
    {
        var values = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new LaunchOptions(
            values.Contains("--background"),
            values.Contains("--safe"),
            values.Contains("--history"),
            values.Contains("--statistics"));
    }
}
