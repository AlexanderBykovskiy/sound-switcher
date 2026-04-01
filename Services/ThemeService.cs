using Microsoft.Win32;

namespace SoundSwitcher.Services;

public static class ThemeService
{
    public static bool IsDarkModeEnabled()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0;
            }
        }
        catch
        {
            // Keep light mode fallback if registry lookup fails.
        }

        return false;
    }
}
