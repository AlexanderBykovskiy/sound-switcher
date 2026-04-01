using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
using SoundSwitcher.Models;
using SoundSwitcher.Services;
using SoundSwitcher.Views;

namespace SoundSwitcher.Tray;

public sealed class TrayController : IDisposable
{
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
        var backgroundKind = ResolveBackground(settings.TrayBackgroundShape);
        var color = ParseColor(settings.TrayBackgroundColorHex);

        var icon = CreateTrayIcon(backgroundKind, color, iconKind);
        _trayIcon?.Dispose();
        _trayIcon = icon;
        _notifyIcon.Icon = _trayIcon;
    }

    private static string ResolveDeviceIcon(AppSettings settings, string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return TrayIconKinds.DeviceVolume;
        }

        if (!settings.DeviceIconById.TryGetValue(deviceId, out var iconKind))
        {
            return TrayIconKinds.DeviceVolume;
        }

        return TrayIconKinds.DeviceValues.Contains(iconKind)
            ? iconKind
            : TrayIconKinds.DeviceVolume;
    }

    private static string ResolveBackground(string? backgroundKind) =>
        TrayIconKinds.BackgroundValues.Contains(backgroundKind ?? string.Empty)
            ? backgroundKind!
            : TrayIconKinds.BackgroundCrop11;

    private static Icon CreateFallbackIcon() =>
        CreateTrayIcon(TrayIconKinds.BackgroundCrop11, Color.Black, TrayIconKinds.DeviceVolume);

    private static Icon CreateTrayIcon(string backgroundKind, Color backgroundColor, string deviceIconKind)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var bgPath = CreateBackgroundPath(backgroundKind);
        using var bgBrush = new SolidBrush(backgroundColor);
        graphics.FillPath(bgBrush, bgPath);

        using var iconPen = new Pen(Color.White, 2f)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        DrawDeviceIcon(graphics, deviceIconKind, iconPen);

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

    private static GraphicsPath CreateBackgroundPath(string backgroundKind)
    {
        var radius = backgroundKind == TrayIconKinds.BackgroundSquareRounded ? 10 : 8;
        return CreateRoundedRectPath(new Rectangle(0, 0, 32, 32), radius);
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void DrawDeviceIcon(Graphics graphics, string iconKind, Pen pen)
    {
        var iconScale = iconKind == TrayIconKinds.DeviceHeadphones ? 1.12f : 1.2f;
        var state = graphics.Save();
        graphics.TranslateTransform(16f, 16f);
        graphics.ScaleTransform(iconScale, iconScale);
        graphics.TranslateTransform(-16f, -16f);

        try
        {
            switch (iconKind)
            {
                case TrayIconKinds.DeviceAirpods:
                    DrawAirpodsIcon(graphics, pen);
                    break;
                case TrayIconKinds.DeviceSpeaker:
                    DrawSpeakerIcon(graphics, pen);
                    break;
                case TrayIconKinds.DeviceMusic:
                    DrawMusicIcon(graphics, pen);
                    break;
                case TrayIconKinds.DeviceHeadphones:
                    DrawHeadphonesIcon(graphics, pen);
                    break;
                default:
                    DrawVolumeIcon(graphics, pen);
                    break;
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static void DrawVolumeIcon(Graphics graphics, Pen pen)
    {
        graphics.DrawLines(
            pen,
            new Point[]
            {
                new(9, 13), new(12, 13), new(16, 9), new(16, 23), new(12, 19), new(9, 19), new(9, 13)
            });
        graphics.DrawArc(pen, 15, 12, 7, 8, -45, 90);
    }

    private static void DrawSpeakerIcon(Graphics graphics, Pen pen)
    {
        using var boxPath = CreateRoundedRectPath(new Rectangle(9, 7, 14, 18), 4);
        graphics.DrawPath(pen, boxPath);
        graphics.DrawEllipse(pen, 13, 14, 6, 6);
        graphics.DrawLine(pen, 16, 10, 16, 10);
    }

    private static void DrawMusicIcon(Graphics graphics, Pen pen)
    {
        graphics.DrawLine(pen, 13, 9, 21, 9);
        graphics.DrawLine(pen, 13, 9, 13, 19);
        graphics.DrawLine(pen, 21, 9, 21, 18);
        graphics.DrawEllipse(pen, 8, 17, 6, 6);
        graphics.DrawEllipse(pen, 18, 16, 6, 6);
    }

    private static void DrawHeadphonesIcon(Graphics graphics, Pen pen)
    {
        graphics.DrawArc(pen, 8, 8, 16, 14, 180, 180);
        using var leftPath = CreateRoundedRectPath(new Rectangle(7, 14, 4, 8), 2);
        graphics.DrawPath(pen, leftPath);
        using var rightPath = CreateRoundedRectPath(new Rectangle(21, 14, 4, 8), 2);
        graphics.DrawPath(pen, rightPath);
    }

    private static void DrawAirpodsIcon(Graphics graphics, Pen pen)
    {
        using var leftPath = CreateRoundedRectPath(new Rectangle(8, 8, 4, 14), 2);
        graphics.DrawPath(pen, leftPath);
        using var rightPath = CreateRoundedRectPath(new Rectangle(20, 8, 4, 14), 2);
        graphics.DrawPath(pen, rightPath);
        graphics.DrawLine(pen, 10, 14, 10, 22);
        graphics.DrawLine(pen, 22, 14, 22, 22);
    }

    private static Color ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Color.Black;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith('#'))
        {
            normalized = $"#{normalized}";
        }

        if (normalized.Length != 7)
        {
            return Color.Black;
        }

        for (var i = 1; i < normalized.Length; i++)
        {
            if (!Uri.IsHexDigit(normalized[i]))
            {
                return Color.Black;
            }
        }

        return ColorTranslator.FromHtml(normalized);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
