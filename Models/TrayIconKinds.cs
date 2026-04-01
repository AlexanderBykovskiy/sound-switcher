namespace SoundSwitcher.Models;

public static class TrayIconKinds
{
    public const string BackgroundCrop11 = "crop-1-1";
    public const string BackgroundSquareRounded = "square-rounded";

    public const string DeviceVolume = "volume";
    public const string DeviceAirpods = "device-airpods";
    public const string DeviceSpeaker = "device-speaker";
    public const string DeviceMusic = "music";
    public const string DeviceHeadphones = "headphones";

    public static readonly string[] BackgroundValues =
    [
        BackgroundCrop11,
        BackgroundSquareRounded
    ];

    public static readonly string[] DeviceValues =
    [
        DeviceVolume,
        DeviceAirpods,
        DeviceSpeaker,
        DeviceMusic,
        DeviceHeadphones
    ];
}
