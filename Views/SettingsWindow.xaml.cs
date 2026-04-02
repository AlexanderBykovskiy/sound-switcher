using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SoundSwitcher.Models;
using SoundSwitcher.Services;
using SoundSwitcher.ViewModels;

namespace SoundSwitcher.Views;

public partial class SettingsWindow : System.Windows.Window
{
    private const double MinTrayIconColumnWidth = 240;

    private bool _updatingListViewColumns;

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
                IconKind = DeviceIconCatalog.ResolveIconForDevice(iconsByDevice, d.Id)
            }));

        DataContext = this;
    }

    private void DeviceListView_OnLoaded(object sender, RoutedEventArgs e) =>
        UpdateListViewColumnWidths();

    private void DeviceListView_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            UpdateListViewColumnWidths();
        }
    }

    /// <summary>
    /// GridView не даёт «*» для колонки: колонка иконки — авто по содержимому, имя устройства — остаток ширины.
    /// </summary>
    private void UpdateListViewColumnWidths()
    {
        if (_updatingListViewColumns || !DeviceListView.IsLoaded ||
            DeviceListView.ActualWidth <= 0 ||
            DeviceListView.View is not GridView gridView ||
            gridView.Columns.Count < 3)
        {
            return;
        }

        _updatingListViewColumns = true;
        try
        {
            var checkboxCol = gridView.Columns[0];
            var deviceCol = gridView.Columns[1];
            var iconCol = gridView.Columns[2];

            iconCol.Width = double.NaN;
            DeviceListView.UpdateLayout();

            const double gridChrome = 12;
            var scrollReserve = SystemParameters.VerticalScrollBarWidth;
            var listWidth = DeviceListView.ActualWidth;

            var checkboxW = checkboxCol.ActualWidth > 0
                ? checkboxCol.ActualWidth
                : (double.IsNaN(checkboxCol.Width) || checkboxCol.Width <= 0 ? 36 : checkboxCol.Width);

            var iconNatural = iconCol.ActualWidth > 0 ? iconCol.ActualWidth : MinTrayIconColumnWidth;
            var iconW = Math.Max(iconNatural, MinTrayIconColumnWidth);
            iconCol.Width = iconW;

            var deviceWidth = listWidth - scrollReserve - gridChrome - checkboxW - iconW;
            if (deviceWidth < 120)
            {
                deviceWidth = 120;
            }

            if (double.IsNaN(deviceCol.Width) || Math.Abs(deviceCol.Width - deviceWidth) > 0.5)
            {
                deviceCol.Width = deviceWidth;
            }
        }
        finally
        {
            _updatingListViewColumns = false;
        }
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
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

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

}
