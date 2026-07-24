namespace WorkRestGuard;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
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
            logger.Error("Unhandled UI exception", eventArgs.Exception);
            context?.EmergencyExit();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            logger.Error("Unhandled process exception", eventArgs.ExceptionObject as Exception);
            context?.EmergencyExit();
        };

        try
        {
            context = new GuardApplicationContext(LaunchOptions.Parse(args), logger);
            Application.Run(context);
            return 0;
        }
        catch (Exception exception)
        {
            logger.Error("Fatal startup exception", exception);
            context?.EmergencyExit();
            MessageBox.Show(
                "Work Rest Guard could not start safely. No overlay was enabled.\n\n" +
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
    }
}
