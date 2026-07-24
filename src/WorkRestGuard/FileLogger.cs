namespace WorkRestGuard;

public sealed class FileLogger : IDisposable
{
    private const long MaximumBytes = 1024 * 1024;
    private readonly object _gate = new();
    private readonly string _path;
    private bool _disposed;

    public FileLogger(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        RotateIfNeeded();
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Warn(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", message, exception);

    public void Dispose() => _disposed = true;

    private void Write(string level, string message, Exception? exception)
    {
        if (_disposed)
        {
            return;
        }

        var safeMessage = Sanitize(message);
        var line = $"{DateTimeOffset.Now:O} [{level}] {safeMessage}";
        if (exception is not null)
        {
            line += $" | {exception.GetType().Name}: {Sanitize(exception.Message)}";
        }

        lock (_gate)
        {
            RotateIfNeeded();
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }

    private void RotateIfNeeded()
    {
        var file = new FileInfo(_path);
        if (!file.Exists || file.Length < MaximumBytes)
        {
            return;
        }

        File.Move(_path, _path + ".1", true);
    }

    private static string Sanitize(string message) =>
        message.Replace('\r', ' ').Replace('\n', ' ');
}
