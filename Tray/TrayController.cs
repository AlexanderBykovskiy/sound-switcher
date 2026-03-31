using System.Drawing;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;
using SoundSwitcher.Services;
using SoundSwitcher.Views;

namespace SoundSwitcher.Tray;

public sealed class TrayController : IDisposable
{
    private readonly AudioDeviceService _audioDeviceService;
    private readonly SettingsService _settingsService;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Icon _trayIcon;

    public TrayController(AudioDeviceService audioDeviceService, SettingsService settingsService)
    {
        _audioDeviceService = audioDeviceService;
        _settingsService = settingsService;
        _menu = BuildMenu();
        _trayIcon = CreateCircleIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Sound Switcher",
            Icon = _trayIcon,
            Visible = false,
            ContextMenuStrip = _menu
        };
    }

    public void Initialize()
    {
        _notifyIcon.MouseClick += OnNotifyIconClick;
        _notifyIcon.Visible = true;
        ShowBalloon("Sound Switcher", "Приложение запущено.");
    }

    public void Dispose()
    {
        _notifyIcon.MouseClick -= OnNotifyIconClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIcon.Dispose();
        _menu.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Настройки", null, (_, _) => ShowSettingsWindow());
        menu.Items.Add("Выход", null, (_, _) => System.Windows.Application.Current.Shutdown());
        return menu;
    }

    private void OnNotifyIconClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            SwitchToNextDevice();
        }
    }

    private void ShowSettingsWindow()
    {
        var devices = _audioDeviceService.GetOutputDevices();
        var settings = _settingsService.Load();

        var window = new SettingsWindow(devices, _settingsService, settings);
        _ = window.ShowDialog();
    }

    private void SwitchToNextDevice()
    {
        var devices = _audioDeviceService.GetOutputDevices();
        if (devices.Count == 0)
        {
            ShowBalloon("Sound Switcher", "Не найдено активных устройств вывода.");
            return;
        }

        var settings = _settingsService.Load();
        var selectedIds = settings.IncludedDeviceIds;

        var rotation = selectedIds.Count == 0
            ? devices.ToList()
            : devices.Where(d => selectedIds.Contains(d.Id, StringComparer.OrdinalIgnoreCase)).ToList();

        if (rotation.Count == 0)
        {
            ShowBalloon("Sound Switcher", "В настройках не выбраны доступные устройства.");
            return;
        }

        var currentId = _audioDeviceService.GetDefaultOutputDeviceId();
        var currentIndex = rotation.FindIndex(d => string.Equals(d.Id, currentId, StringComparison.OrdinalIgnoreCase));
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % rotation.Count;
        var next = rotation[nextIndex];

        _audioDeviceService.SetDefaultOutputDevice(next.Id);
        ShowBalloon("Sound Switcher", $"Текущее устройство: {next.Name}");
    }

    private void ShowBalloon(string title, string text)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(1500);
    }

    private static Icon CreateCircleIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
        graphics.FillEllipse(shadowBrush, 4, 5, 24, 24);

        using var circleBrush = new SolidBrush(Color.FromArgb(255, 0, 170, 255));
        graphics.FillEllipse(circleBrush, 3, 3, 24, 24);

        using var highlightBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
        graphics.FillEllipse(highlightBrush, 8, 7, 8, 8);

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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
