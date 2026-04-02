using System.Collections.ObjectModel;
using SoundSwitcher.Models;
using SoundSwitcher.Services;
using SoundSwitcher.ViewModels;

namespace SoundSwitcher.Views;

public partial class SettingsWindow : System.Windows.Window
{
    private readonly SettingsService _settingsService;

    public ObservableCollection<DeviceSelectionItem> Devices { get; }

    public IReadOnlyList<DeviceIconOption> DeviceIconOptions { get; } = BuildDeviceIconOptions();

    private static IReadOnlyList<DeviceIconOption> BuildDeviceIconOptions() =>
        DeviceIconCatalog.PngFileNames.Select(fileName => new DeviceIconOption
        {
            Key = fileName,
            Label = fileName,
            Preview = IconPreviewHelper.LoadDeviceIconPng(fileName)
        }).ToList();

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

        DataContext = this;
    }

    private void OnSaveClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        var selectedIds = Devices
            .Where(d => d.IsIncluded)
            .Select(d => d.Id)
            .ToList();

        var iconById = Devices.ToDictionary(
            d => d.Id,
            d => DeviceIconCatalog.NormalizeStoredKey(d.IconKind),
            StringComparer.OrdinalIgnoreCase);

        _settingsService.Save(new AppSettings
        {
            IncludedDeviceIds = selectedIds,
            DeviceIconById = iconById
        });

        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string ResolveDeviceIcon(
        IReadOnlyDictionary<string, string> iconsByDevice,
        string deviceId)
    {
        if (!iconsByDevice.TryGetValue(deviceId, out var iconKind))
        {
            return DeviceIconCatalog.DefaultIconFileName;
        }

        return DeviceIconCatalog.NormalizeStoredKey(iconKind);
    }

}
