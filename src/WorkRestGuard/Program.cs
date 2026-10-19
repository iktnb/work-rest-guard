namespace WorkRestGuard;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--stop-for-update", StringComparer.OrdinalIgnoreCase))
        {
            if (!EventWaitHandle.TryOpenExisting(AppIdentity.UpdateStopEventName, out var signal)) return 2;
            using (signal) signal.Set();
            return 0;
        }
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(true, AppIdentity.MutexName, out var ownsMutex);
        if (!ownsMutex)
        {
            SingleInstanceSignal.SignalExistingInstance();
            return 0;
        }

        using var logger = new FileLogger(AppPaths.LogPath);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        GuardApplicationContext? context = null;
        Application.ThreadException += (_, eventArgs) =>
        {
            LogError("Unhandled UI exception", eventArgs.Exception);
            context?.RecoverUiAfterError();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            LogError("Unhandled process exception", eventArgs.ExceptionObject as Exception);
            context?.FlushUsage();
        };

        try
        {
            context = new GuardApplicationContext(LaunchOptions.Parse(args), logger);
            Application.Run(context);
            return 0;
        }
        catch (Exception exception)
        {
            LogError("Fatal startup exception", exception);
            context?.FlushUsage();
            MessageBox.Show(
                "Work Rest Guard could not start.\n\n" +
                "See the local log:\n" + AppPaths.LogPath,
                AppIdentity.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            context?.Dispose();
        }

        void LogError(string message, Exception? exception)
        {
            try { logger.Error(message, exception); }
            catch (Exception loggingError) when (loggingError is IOException or UnauthorizedAccessException)
            { /* A log failure must not stop statistics recording. */ }
        }
    }
}
