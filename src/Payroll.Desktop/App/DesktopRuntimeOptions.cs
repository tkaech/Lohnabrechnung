using System.Text.Json;

namespace Payroll.Desktop.Bootstrapping;

public sealed record DesktopRuntimeOptions(
    string EnvironmentName,
    string DatabasePath);

public static class DesktopRuntimeOptionsLoader
{
    private const string DefaultEnvironmentName = "Development";
    private const string DatabasePathArgumentPrefix = "--db-path=";
    private const string EnvironmentArgumentPrefix = "--environment=";

    public static DesktopRuntimeOptions Load(string[] args)
    {
        var environmentName = ResolveEnvironmentName(args);
        var appSettings = LoadAppSettings(environmentName);
        var configuredPath = ResolveConfiguredDatabasePath(appSettings, environmentName);
        var databasePathOverride = ResolveDatabasePathOverride(args);
        var databasePath = ResolvePath(databasePathOverride ?? configuredPath);

        return new DesktopRuntimeOptions(environmentName, databasePath);
    }

    private static string ResolveEnvironmentName(string[] args)
    {
        var argumentValue = args
            .FirstOrDefault(item => item.StartsWith(EnvironmentArgumentPrefix, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(argumentValue))
        {
            return argumentValue[EnvironmentArgumentPrefix.Length..].Trim();
        }

        return Environment.GetEnvironmentVariable("PAYROLLAPP_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? DefaultEnvironmentName;
    }

    private static AppSettingsFile LoadAppSettings(string environmentName)
    {
        var combined = new AppSettingsFile();
        MergeInto(combined, ReadAppSettingsFile("appsettings.json"));
        MergeInto(combined, ReadAppSettingsFile($"appsettings.{environmentName}.json"));
        return combined;
    }

    private static AppSettingsFile? ReadAppSettingsFile(string fileName)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var json = File.ReadAllText(fullPath);
        return JsonSerializer.Deserialize<AppSettingsFile>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static void MergeInto(AppSettingsFile target, AppSettingsFile? source)
    {
        if (source?.Database is null)
        {
            return;
        }

        target.Database ??= new DatabaseSettingsSection();
        if (!string.IsNullOrWhiteSpace(source.Database.Path))
        {
            target.Database.Path = source.Database.Path;
        }
    }

    private static string ResolveConfiguredDatabasePath(AppSettingsFile appSettings, string environmentName)
    {
        var configuredPath = appSettings.Database?.Path;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var defaultFileName = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            ? "payroll.localdev.db"
            : "payroll.test.db";

        return "{LocalAppData}/PayrollApp/" + defaultFileName;
    }

    private static string? ResolveDatabasePathOverride(string[] args)
    {
        var argumentValue = args
            .FirstOrDefault(item => item.StartsWith(DatabasePathArgumentPrefix, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(argumentValue))
        {
            return argumentValue[DatabasePathArgumentPrefix.Length..].Trim();
        }

        return Environment.GetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH");
    }

    private static string ResolvePath(string rawPath)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var resolvedPath = rawPath
            .Replace("{LocalAppData}", localAppData, StringComparison.OrdinalIgnoreCase)
            .Replace("{UserProfile}", userProfile, StringComparison.OrdinalIgnoreCase)
            .Replace("{AppBaseDirectory}", AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase);

        return Path.GetFullPath(resolvedPath);
    }

    private sealed class AppSettingsFile
    {
        public DatabaseSettingsSection? Database { get; set; }
    }

    private sealed class DatabaseSettingsSection
    {
        public string? Path { get; set; }
    }
}
