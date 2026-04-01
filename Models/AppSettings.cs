namespace SoundSwitcher.Models;

public sealed class AppSettings
{
    public List<string> IncludedDeviceIds { get; init; } = [];
    public string TrayBackgroundShape { get; init; } = TrayIconKinds.BackgroundCrop11;
    public string TrayBackgroundColorHex { get; init; } = "#000000";
    public Dictionary<string, string> DeviceIconById { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
