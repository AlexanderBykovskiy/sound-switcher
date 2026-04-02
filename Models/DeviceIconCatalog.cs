namespace SoundSwitcher.Models;

public static class DeviceIconCatalog
{
    public const string DefaultIconFileName = "speaker-monitor.png";

    /// <summary>Имена файлов в <c>Assets/DeviceIcons</c>, порядок в списке настроек.</summary>
    public static readonly IReadOnlyList<string> PngFileNames =
    [
        "speaker-monitor.png",
        "bass.png",
        "boombox.png",
        "earbuds-case.png",
        "earbuds.png",
        "headphone-combo.png",
        "headphone-dark.png",
        "headphone-light.png",
        "headphone-pink.png",
        "headset.png",
        "ipod.png",
        "mixer.png",
        "player.png",
        "speaker-bluetooth.png",
        "speaker-passive.png",
        "turntable.png"
    ];

    public static string MigrateLegacyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return DefaultIconFileName;
        }

        var k = key.Trim();
        if (string.Equals(k, "volume", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultIconFileName;
        }

        return k switch
        {
            TrayIconKinds.DeviceAirpods or "device-airpods" => "earbuds.png",
            TrayIconKinds.DeviceSpeaker or "device-speaker" => "speaker-bluetooth.png",
            TrayIconKinds.DeviceMusic or "music" => "ipod.png",
            TrayIconKinds.DeviceHeadphones or "headphones" => "headphone-light.png",
            _ => k
        };
    }

    public static bool IsValidKey(string key) =>
        PngFileNames.Contains(key, StringComparer.OrdinalIgnoreCase);

    public static string NormalizeStoredKey(string? key)
    {
        var migrated = MigrateLegacyKey(key);
        return IsValidKey(migrated) ? migrated : DefaultIconFileName;
    }
}
