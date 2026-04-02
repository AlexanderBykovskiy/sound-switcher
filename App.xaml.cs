using System.Threading;
using System.Windows;
using SoundSwitcher.Services;
using SoundSwitcher.Tray;

namespace SoundSwitcher;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\SoundSwitcher.SingleInstance.7C9B1E2F";

    private static Mutex? _singleInstanceMutex;
    private TrayController? _trayController;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (!TryTakeSingleInstanceOwnership())
        {
            Shutdown();
            return;
        }

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
                $"SoundSwitcher startup error:{Environment.NewLine}{ex.Message}",
                "SoundSwitcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayController?.Dispose();
        ReleaseSingleInstanceMutex();
        base.OnExit(e);
    }

    private static bool TryTakeSingleInstanceOwnership()
    {
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void ReleaseSingleInstanceMutex()
    {
        if (_singleInstanceMutex is null)
        {
            return;
        }

        try
        {
            _singleInstanceMutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // владение mutex уже снято
        }

        _singleInstanceMutex.Dispose();
        _singleInstanceMutex = null;
    }
}
