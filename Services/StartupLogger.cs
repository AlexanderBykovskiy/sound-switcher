using System.IO;

namespace SoundSwitcher.Services;

public static class StartupLogger
{
    private const int MaxLogFileBytes = 256 * 1024;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SoundSwitcher",
        "startup.log");

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(Exception exception, string context)
    {
        var text = $"{context}{Environment.NewLine}{exception}";
        Write("ERROR", text);
    }

    private static void Write(string level, string message)
    {
        var dir = Path.GetDirectoryName(LogPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        RotateLogIfNeeded();

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
        File.AppendAllText(LogPath, line);
    }

    private static void RotateLogIfNeeded()
    {
        try
        {
            if (!File.Exists(LogPath))
            {
                return;
            }

            var info = new FileInfo(LogPath);
            if (info.Length < MaxLogFileBytes)
            {
                return;
            }

            var backup = LogPath + ".old";
            if (File.Exists(backup))
            {
                File.Delete(backup);
            }

            File.Move(LogPath, backup);
        }
        catch
        {
            // не блокируем работу приложения из-за лога
        }
    }
}
