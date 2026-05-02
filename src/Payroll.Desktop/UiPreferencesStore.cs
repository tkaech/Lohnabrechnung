using System.Text.Json;

namespace Payroll.Desktop;

internal static class UiPreferencesStore
{
    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static UiPreferences? _cache;

    public static UiPreferences Load()
    {
        lock (SyncRoot)
        {
            EnsureCacheLoaded();
            return _cache!;
        }
    }

    public static void Save(UiPreferences preferences)
    {
        lock (SyncRoot)
        {
            _cache = preferences;
            var directory = GetSettingsDirectory();
            Directory.CreateDirectory(directory);
            File.WriteAllText(GetSettingsPath(), JsonSerializer.Serialize(preferences, SerializerOptions));
        }
    }

    private static void EnsureCacheLoaded()
    {
        if (_cache is not null)
        {
            return;
        }

        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            _cache = new UiPreferences();
            return;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            _cache = JsonSerializer.Deserialize<UiPreferences>(json, SerializerOptions) ?? new UiPreferences();
        }
        catch
        {
            _cache = new UiPreferences();
        }
    }

    private static string GetSettingsPath() => Path.Combine(GetSettingsDirectory(), "ui-preferences.json");

    private static string GetSettingsDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share");
        }

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = Path.GetTempPath();
        }

        return Path.Combine(localAppData, "PayrollApp");
    }
}

internal sealed class UiPreferences
{
    public double ZoomFactor { get; set; } = 1d;
}
