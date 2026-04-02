using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SoundSwitcher;

/// <summary>
/// Масштабирование иконок устройств в GDI / WPF. Пиксели рисуются как в файле (в т.ч. альфа у PNG), без вырезания по «тёмному фону» —
/// иначе чёрные детали корпуса ошибочно становились прозрачными.
/// </summary>
internal static class DeviceIconPngDrawing
{
    public static void DrawDevicePngOnto(Graphics graphics, Image image, RectangleF destinationRect)
    {
        var dest = Rectangle.Round(destinationRect);
        graphics.DrawImage(image, dest, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
    }

    public static System.Windows.Media.ImageSource? LoadAsTransparentBitmapSource(string path, int maxEdgePixels)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var src = Image.FromFile(path);
        var scale = Math.Min(1.0, maxEdgePixels / (double)Math.Max(src.Width, src.Height));
        var tw = Math.Max(1, (int)(src.Width * scale));
        var th = Math.Max(1, (int)(src.Height * scale));
        using var buffer = new Bitmap(tw, th, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(buffer))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            DrawDevicePngOnto(g, src, new Rectangle(0, 0, tw, th));
        }

        var hBitmap = buffer.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);
}
