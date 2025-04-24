using System;
using System.Threading;
using System.Threading.Tasks;
using PluginInterface;
using SkiaSharp;

namespace BWPlugin;

public class BwPlugin : IPlugin
{
    public string Name { get; } = "Black&White";
    public string Author { get; } = "Vladimir";
    
    private int _delay = 500;

    private double percent;

    public async Task Transform(SKBitmap bitmap, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        await Task.Run(() => ApplyBlackAndWhite(bitmap, progress, cancellationToken), cancellationToken);
        if (!cancellationToken.IsCancellationRequested)
        {
            bitmap.NotifyPixelsChanged();
        }
    }

    private unsafe void ApplyBlackAndWhite(SKBitmap bitmap, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        int height = bitmap.Height;
        int width = bitmap.Width;
        int bytesPerPixel = bitmap.BytesPerPixel;
        SKColorType colorType = bitmap.ColorType;
        IntPtr pixelsPtr = bitmap.GetPixels();
        byte* ptr = (byte*)pixelsPtr.ToPointer();

        // Параллельная обработка строк изображения
        Parallel.For(0, height, new ParallelOptions { CancellationToken = cancellationToken }, y =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return; // Прерываем обработку строки при отмене
            }

            byte* rowPtr = ptr + y * bitmap.RowBytes;

            // Обработка пикселей в строке
            for (int x = 0; x < width; x++)
            {
                byte* pixelPtr = rowPtr + x * bytesPerPixel;

                byte r, g, b, a;
                if (colorType == SKColorType.Bgra8888)
                {
                    b = pixelPtr[0];
                    g = pixelPtr[1];
                    r = pixelPtr[2];
                    a = pixelPtr[3];
                }
                else
                {
                    r = pixelPtr[0];
                    g = pixelPtr[1];
                    b = pixelPtr[2];
                    a = pixelPtr[3];
                }

                // Преобразование в серый цвет
                double gray = 0.299 * r + 0.587 * g + 0.114 * b;
                byte grayByte = (byte)Math.Clamp(gray, 0, 255);

                pixelPtr[0] = grayByte;
                pixelPtr[1] = grayByte;
                pixelPtr[2] = grayByte;
                pixelPtr[3] = a;
            }

            // Обновление прогресса
            Thread.Sleep(_delay);
            percent +=  100d / height;
            progress?.Report((int)percent);
        });
    }
}