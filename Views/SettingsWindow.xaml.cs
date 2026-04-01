using System.Collections.ObjectModel;
using System.Windows;
using Forms = System.Windows.Forms;
using SoundSwitcher.Models;
using SoundSwitcher.Services;
using SoundSwitcher.ViewModels;

namespace SoundSwitcher.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private string _selectedBackgroundShape = TrayIconKinds.BackgroundCrop11;
    private string _backgroundColorHex = "#000000";

    public ObservableCollection<DeviceSelectionItem> Devices { get; }
    public IReadOnlyList<KeyValuePair<string, string>> BackgroundOptions { get; } =
    [
        new(TrayIconKinds.BackgroundCrop11, "crop-1-1"),
        new(TrayIconKinds.BackgroundSquareRounded, "square-rounded")
    ];

    public IReadOnlyList<KeyValuePair<string, string>> DeviceIconOptions { get; } =
    [
        new(TrayIconKinds.DeviceVolume, "volume"),
        new(TrayIconKinds.DeviceAirpods, "device-airpods"),
        new(TrayIconKinds.DeviceSpeaker, "device-speaker"),
        new(TrayIconKinds.DeviceMusic, "music"),
        new(TrayIconKinds.DeviceHeadphones, "headphones")
    ];

    public string SelectedBackgroundShape
    {
        get => _selectedBackgroundShape;
        set => _selectedBackgroundShape = value;
    }

    public string BackgroundColorHex
    {
        get => _backgroundColorHex;
        set => _backgroundColorHex = value;
    }

    public SettingsWindow(
        IReadOnlyList<AudioDeviceInfo> allDevices,
        SettingsService settingsService,
        AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;

        var selected = settings.IncludedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var iconsByDevice = settings.DeviceIconById;
        Devices = new ObservableCollection<DeviceSelectionItem>(
            allDevices.Select(d => new DeviceSelectionItem
            {
                Id = d.Id,
                Name = d.Name,
                IsIncluded = selected.Contains(d.Id),
                IconKind = ResolveDeviceIcon(iconsByDevice, d.Id)
            }));

        SelectedBackgroundShape = TrayIconKinds.BackgroundValues.Contains(settings.TrayBackgroundShape)
            ? settings.TrayBackgroundShape
            : TrayIconKinds.BackgroundCrop11;
        BackgroundColorHex = NormalizeHexColor(settings.TrayBackgroundColorHex);

        DataContext = this;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var selectedIds = Devices
            .Where(d => d.IsIncluded)
            .Select(d => d.Id)
            .ToList();

        var iconById = Devices.ToDictionary(
            d => d.Id,
            d => TrayIconKinds.DeviceValues.Contains(d.IconKind) ? d.IconKind : TrayIconKinds.DeviceVolume,
            StringComparer.OrdinalIgnoreCase);

        _settingsService.Save(new AppSettings
        {
            IncludedDeviceIds = selectedIds,
            TrayBackgroundShape = TrayIconKinds.BackgroundValues.Contains(SelectedBackgroundShape)
                ? SelectedBackgroundShape
                : TrayIconKinds.BackgroundCrop11,
            TrayBackgroundColorHex = NormalizeHexColor(BackgroundColorHex),
            DeviceIconById = iconById
        });

        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnPickColorClicked(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            SolidColorOnly = false
        };

        var initial = NormalizeHexColor(BackgroundColorHex);
        dialog.Color = System.Drawing.ColorTranslator.FromHtml(initial);

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        var hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        BackgroundColorHex = hex;
        ColorHexTextBox.Text = hex;
    }

    private static string ResolveDeviceIcon(
        IReadOnlyDictionary<string, string> iconsByDevice,
        string deviceId)
    {
        if (!iconsByDevice.TryGetValue(deviceId, out var iconKind))
        {
            return TrayIconKinds.DeviceVolume;
        }

        return TrayIconKinds.DeviceValues.Contains(iconKind)
            ? iconKind
            : TrayIconKinds.DeviceVolume;
    }

    private static string NormalizeHexColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "#000000";
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        if (normalized.Length != 7)
        {
            return "#000000";
        }

        for (var i = 1; i < normalized.Length; i++)
        {
            if (!Uri.IsHexDigit(normalized[i]))
            {
                return "#000000";
            }
        }

        return normalized.ToUpperInvariant();
    }
}
