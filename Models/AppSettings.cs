namespace SoundSwitcher.Models;

public sealed class AppSettings
{
    public List<string> IncludedDeviceIds { get; init; } = [];
    public Dictionary<string, string> DeviceIconById { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
