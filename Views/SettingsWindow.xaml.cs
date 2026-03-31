using System.Collections.ObjectModel;
using System.Windows;
using SoundSwitcher.Models;
using SoundSwitcher.Services;
using SoundSwitcher.ViewModels;

namespace SoundSwitcher.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;

    public ObservableCollection<DeviceSelectionItem> Devices { get; }

    public SettingsWindow(
        IReadOnlyList<AudioDeviceInfo> allDevices,
        SettingsService settingsService,
        AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;

        var selected = settings.IncludedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Devices = new ObservableCollection<DeviceSelectionItem>(
            allDevices.Select(d => new DeviceSelectionItem
            {
                Id = d.Id,
                Name = d.Name,
                IsIncluded = selected.Contains(d.Id)
            }));

        DataContext = this;
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var selectedIds = Devices
            .Where(d => d.IsIncluded)
            .Select(d => d.Id)
            .ToList();

        _settingsService.Save(new AppSettings
        {
            IncludedDeviceIds = selectedIds
        });

        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
