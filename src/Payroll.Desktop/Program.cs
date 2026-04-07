using Avalonia;

namespace Payroll.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            var logPath = StartupErrorLogger.WriteStartupErrorLog(exception);
            throw new InvalidOperationException($"{StartupErrorLogger.BuildStartupErrorMessage(exception)} | startup log: {logPath}", exception);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
