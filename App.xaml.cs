using System.Windows;
using SoundSwitcher.Services;
using SoundSwitcher.Tray;

namespace SoundSwitcher;

public partial class App : System.Windows.Application
{
    private TrayController? _trayController;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupLogger.Info("Application startup requested.");

        try
        {
            var settingsService = new SettingsService();
            var audioService = new AudioDeviceService();
            _trayController = new TrayController(audioService, settingsService);
            _trayController.Initialize();
            StartupLogger.Info("Tray initialized.");
        }
        catch (Exception ex)
        {
            StartupLogger.Error(ex, "Unhandled startup exception.");
            System.Windows.MessageBox.Show(
                $"Ошибка запуска SoundSwitcher:{Environment.NewLine}{ex.Message}",
                "SoundSwitcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayController?.Dispose();
        base.OnExit(e);
    }
}
