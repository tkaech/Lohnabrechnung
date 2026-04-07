using System.Text;

namespace Payroll.Desktop;

internal static class StartupErrorLogger
{
    public static string BuildStartupErrorMessage(Exception exception)
    {
        var builder = new StringBuilder("Application startup failed");
        var current = exception;
        var depth = 0;

        while (current is not null)
        {
            builder.Append(depth == 0 ? ": " : " | ");
            builder.Append(current.GetType().Name);
            builder.Append(": ");
            builder.Append(current.Message);

            current = current.InnerException;
            depth++;
        }

        return builder.ToString();
    }

    public static string WriteStartupErrorLog(Exception exception)
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share");
        }

        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Path.GetTempPath();
        }

        var logDirectory = Path.Combine(baseDirectory, "PayrollApp");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "startup-error.log");
        File.WriteAllText(logPath, exception.ToString());
        return logPath;
    }
}
