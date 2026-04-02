using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
using SoundSwitcher;
using SoundSwitcher.Models;
using SoundSwitcher.Services;
using SoundSwitcher.Views;

namespace SoundSwitcher.Tray;

public sealed class TrayController : IDisposable
{
    private const int TrayIconSizePx = 32;
    /// <summary>Скругление углов HICON в трее (пиксели при размере 32×32).</summary>
    private const int TrayIconCornerRadiusPx = 5;

    private readonly AudioDeviceService _audioDeviceService;
    private readonly SettingsService _settingsService;
    private readonly Forms.NotifyIcon _notifyIcon;
    private TrayMenuWindow? _trayMenuWindow;
    private SettingsWindow? _settingsWindow;
    private Icon? _trayIcon;

    public TrayController(AudioDeviceService audioDeviceService, SettingsService settingsService)
    {
        _audioDeviceService = audioDeviceService;
        _settingsService = settingsService;
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Sound Switcher",
            Icon = CreateFallbackIcon(),
            Visible = false
        };
    }

    public void Initialize()
    {
        _notifyIcon.MouseClick += OnNotifyIconClick;
        UpdateTrayIconForCurrentDevice();
        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        _notifyIcon.MouseClick -= OnNotifyIconClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayMenuWindow?.Close();
        _trayIcon?.Dispose();
    }

    private void OnNotifyIconClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            SwitchToNextDevice();
            return;
        }

        if (e.Button == Forms.MouseButtons.Right)
        {
            RunOnUiThread(ShowTrayMenu);
        }
    }

    private void ShowSettingsWindow()
    {
        RunOnUiThread(() =>
        {
            try
            {
                if (_settingsWindow is not null)
                {
                    if (_settingsWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _settingsWindow.WindowState = System.Windows.WindowState.Normal;
                    }

                    _settingsWindow.Activate();
                    _settingsWindow.Focus();
                    return;
                }

                var devices = _audioDeviceService.GetOutputDevices();
                var settings = _settingsService.Load();

                _settingsWindow = new SettingsWindow(devices, _settingsService, settings)
                {
                    Topmost = true
                };
                _settingsWindow.Closed += OnSettingsWindowClosed;

                var saved = _settingsWindow.ShowDialog() == true;
                if (saved)
                {
                    UpdateTrayIconForCurrentDevice();
                }
            }
            catch (Exception ex)
            {
                StartupLogger.Error(ex, "Failed to open Settings window.");
                System.Windows.MessageBox.Show(
                    $"Failed to open Settings.{Environment.NewLine}{ex.Message}",
                    "SoundSwitcher",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        });
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (_settingsWindow is null)
        {
            return;
        }

        _settingsWindow.Closed -= OnSettingsWindowClosed;
        _settingsWindow = null;
    }

    private void ShowTrayMenu()
    {
        try
        {
            var cursor = Forms.Control.MousePosition;
            var darkMode = ThemeService.IsDarkModeEnabled();

            _trayMenuWindow?.Close();
            _trayMenuWindow = new TrayMenuWindow(
                settingsAction: ShowSettingsWindow,
                exitAction: () => RunOnUiThread(() => System.Windows.Application.Current.Shutdown()),
                darkMode: darkMode);

            _trayMenuWindow.Show();
            _trayMenuWindow.Left = Math.Max(0, cursor.X - _trayMenuWindow.ActualWidth + 4);
            _trayMenuWindow.Top = Math.Max(0, cursor.Y - _trayMenuWindow.ActualHeight - 8);
            _trayMenuWindow.Activate();
        }
        catch (Exception ex)
        {
            StartupLogger.Error(ex, "Failed to open tray menu.");
            System.Windows.MessageBox.Show(
                $"Failed to open tray menu.{Environment.NewLine}{ex.Message}",
                "SoundSwitcher",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher is null || app.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        app.Dispatcher.Invoke(action);
    }

    private void SwitchToNextDevice()
    {
        var devices = _audioDeviceService.GetOutputDevices();
        if (devices.Count == 0)
        {
            ShowBalloon("Sound Switcher", "No active output devices found.");
            return;
        }

        var settings = _settingsService.Load();
        var selectedIds = settings.IncludedDeviceIds;

        var rotation = selectedIds.Count == 0
            ? devices.ToList()
            : devices.Where(d => selectedIds.Contains(d.Id, StringComparer.OrdinalIgnoreCase)).ToList();

        if (rotation.Count == 0)
        {
            ShowBalloon("Sound Switcher", "No available devices selected in settings.");
            return;
        }

        var currentId = _audioDeviceService.GetDefaultOutputDeviceId();
        var currentIndex = rotation.FindIndex(d => string.Equals(d.Id, currentId, StringComparison.OrdinalIgnoreCase));
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % rotation.Count;
        var next = rotation[nextIndex];

        _audioDeviceService.SetDefaultOutputDevice(next.Id);
        UpdateTrayIcon(next.Id);
    }

    private void ShowBalloon(string title, string text)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(1500);
    }

    private void UpdateTrayIconForCurrentDevice()
    {
        var currentId = _audioDeviceService.GetDefaultOutputDeviceId();
        UpdateTrayIcon(currentId);
    }

    private void UpdateTrayIcon(string? currentDeviceId)
    {
        var settings = _settingsService.Load();
        var iconKind = ResolveDeviceIcon(settings, currentDeviceId);
        var icon = CreateTrayIcon(iconKind);
        _trayIcon?.Dispose();
        _trayIcon = icon;
        _notifyIcon.Icon = _trayIcon;
    }

    private static string ResolveDeviceIcon(AppSettings settings, string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return DeviceIconCatalog.DefaultIconFileName;
        }

        if (!settings.DeviceIconById.TryGetValue(deviceId, out var iconKind))
        {
            return DeviceIconCatalog.DefaultIconFileName;
        }

        return DeviceIconCatalog.NormalizeStoredKey(iconKind);
    }

    private static Icon CreateFallbackIcon() =>
        CreateTrayIcon(DeviceIconCatalog.DefaultIconFileName);

    private static Icon CreateTrayIcon(string deviceIconKind)
    {
        using var bitmap = new Bitmap(TrayIconSizePx, TrayIconSizePx);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, TrayIconSizePx, TrayIconSizePx);
        using var clip = CreateRoundedRectPath(bounds, TrayIconCornerRadiusPx);
        graphics.SetClip(clip);
        try
        {
            DrawDeviceIcon(graphics, deviceIconKind);
        }
        finally
        {
            graphics.ResetClip();
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static void DrawDeviceIcon(Graphics graphics, string iconKind)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "DeviceIcons", iconKind);
        if (!File.Exists(path))
        {
            if (!string.Equals(iconKind, DeviceIconCatalog.DefaultIconFileName, StringComparison.OrdinalIgnoreCase))
            {
                DrawDeviceIcon(graphics, DeviceIconCatalog.DefaultIconFileName);
            }

            return;
        }

        try
        {
            using var image = Image.FromFile(path);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.CompositingMode = CompositingMode.SourceOver;
            DeviceIconPngDrawing.DrawDevicePngOnto(
                graphics,
                image,
                new RectangleF(0f, 0f, TrayIconSizePx, TrayIconSizePx));
        }
        catch
        {
            if (!string.Equals(iconKind, DeviceIconCatalog.DefaultIconFileName, StringComparison.OrdinalIgnoreCase))
            {
                DrawDeviceIcon(graphics, DeviceIconCatalog.DefaultIconFileName);
            }
        }
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
