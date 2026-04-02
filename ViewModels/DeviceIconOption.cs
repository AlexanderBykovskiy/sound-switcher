using System.Windows.Media;

namespace SoundSwitcher.ViewModels;

public sealed class DeviceIconOption
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public ImageSource? Preview { get; init; }
}
