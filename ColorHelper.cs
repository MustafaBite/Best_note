using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NOT_VE_GÜNLÜK
{
    public static class ColorHelper
    {
        public static bool IsImageDark(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return true; // Default to dark (white text) if no background or error

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.DecodePixelWidth = 50; // Performance: Analyze small thumbnail
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                return IsBitmapDark(bitmap);
            }
            catch
            {
                return true; // Default fallback
            }
        }

        public static bool IsBitmapDark(BitmapSource bitmap)
        {
            // Force conversion to Bgra32 to ensure 4 bytes per pixel for analysis
            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            
            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            converted.CopyPixels(pixels, stride, 0);

            double totalBrightness = 0;
            int sampleCount = 0;

            // Sample pixels with a step to improve performance while covering the whole image
            int step = Math.Max(1, width / 50); 

            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int index = y * stride + x * 4;
                    if (index + 2 >= pixels.Length) continue;

                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];

                    // Perceived luminance formula
                    double brightness = (0.299 * r) + (0.587 * g) + (0.114 * b);
                    totalBrightness += brightness;
                    sampleCount++;
                }
            }

            if (sampleCount == 0) return true;

            double averageBrightness = totalBrightness / sampleCount;
            
            // Threshold: If average brightness is > 160 (fairly bright), use black text (isDark = false)
            // If < 160, use white text (isDark = true)
            return averageBrightness < 160; 
        }

        public static bool IsColorDark(Color color)
        {
            // Luminance formula: 0.299*R + 0.587*G + 0.114*B
            double brightness = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
            return brightness < 128;
        }
    }
}
