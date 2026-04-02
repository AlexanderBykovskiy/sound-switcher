using System.IO;
using System.Text.Json;
using SoundSwitcher.Models;

namespace SoundSwitcher.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SoundSwitcher");
        Directory.CreateDirectory(appDir);
        _settingsPath = Path.Combine(appDir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            return new AppSettings
            {
                IncludedDeviceIds = settings.IncludedDeviceIds ?? [],
                DeviceIconById = settings.DeviceIconById ?? new(StringComparer.OrdinalIgnoreCase)
            };
        }
        catch (Exception ex)
        {
            StartupLogger.Error(ex, "Failed to load settings; using defaults.");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
