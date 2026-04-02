using System.IO;
using System.Windows.Media;

namespace SoundSwitcher;

internal static class IconPreviewHelper
{
    public static ImageSource? LoadDeviceIconPng(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "DeviceIcons", fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        return DeviceIconPngDrawing.LoadAsTransparentBitmapSource(path, maxEdgePixels: 44);
    }
}
