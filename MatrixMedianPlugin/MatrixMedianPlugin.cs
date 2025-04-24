using PluginInterface;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MatrixMedianPlugin;

public class MatrixMedianPlugin : IPlugin
{
    public string Name { get; } = "MatrixMedian";
    public string Author { get; } = "Vladimir";

    private const int Radius = 1;
    
    private int _delay = 100;
    
    private double percent;

    public async Task Transform(SKBitmap bitmap, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        SKBitmap copy = bitmap.Copy();
        await Task.Run(() =>
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int kernelSize = (2 * Radius + 1) * (2 * Radius + 1);

            using SKBitmap sourceBitmap = bitmap.Copy();

            IntPtr sourcePixelsPtr = sourceBitmap.GetPixels();
            IntPtr destPixelsPtr = bitmap.GetPixels();

            bool isBgra = bitmap.ColorType == SKColorType.Bgra8888;
            int rIndex = isBgra ? 2 : 0;
            int gIndex = 1;
            int bIndex = isBgra ? 0 : 2;
            int aIndex = 3;

            int bytesPerPixel = bitmap.BytesPerPixel;
            int stride = bitmap.RowBytes;

            
            try
            {
                Parallel.For(0, height, new ParallelOptions() { CancellationToken = cancellationToken }, y =>
                {
                    List<byte> rValues = new(kernelSize);
                    List<byte> gValues = new(kernelSize);
                    List<byte> bValues = new(kernelSize);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }

                    for (int x = 0; x < width; x++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            return;
                        }

                        rValues.Clear();
                        gValues.Clear();
                        bValues.Clear();

                        for (int ky = -Radius; ky <= Radius; ky++)
                        {
                            for (int kx = -Radius; kx <= Radius; kx++)
                            {
                                int nx = Math.Clamp(x + kx, 0, width - 1);
                                int ny = Math.Clamp(y + ky, 0, height - 1);

                                IntPtr neighborPixelPtr = sourcePixelsPtr + (ny * stride) + (nx * bytesPerPixel);
                                bValues.Add(Marshal.ReadByte(neighborPixelPtr, bIndex));
                                gValues.Add(Marshal.ReadByte(neighborPixelPtr, gIndex));
                                rValues.Add(Marshal.ReadByte(neighborPixelPtr, rIndex));
                            }
                        }

                        rValues.Sort();
                        gValues.Sort();
                        bValues.Sort();

                        int medianIndex = kernelSize / 2;
                        byte medianR = rValues[medianIndex];
                        byte medianG = gValues[medianIndex];
                        byte medianB = bValues[medianIndex];

                        IntPtr destPixelPtr = destPixelsPtr + (y * stride) + (x * bytesPerPixel);
                        IntPtr sourceCenterPixelPtr = sourcePixelsPtr + (y * stride) + (x * bytesPerPixel);
                        byte alpha = Marshal.ReadByte(sourceCenterPixelPtr, aIndex);

                        Marshal.WriteByte(destPixelPtr, bIndex, medianB);
                        Marshal.WriteByte(destPixelPtr, gIndex, medianG);
                        Marshal.WriteByte(destPixelPtr, rIndex, medianR);
                        Marshal.WriteByte(destPixelPtr, aIndex, alpha);
                    }

                    Thread.Sleep(_delay);
                    percent += 100d / height;
                    progress?.Report((int)percent);
                });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка фильтрации: " + ex.Message);
            }
        });

        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        else
        {
            bitmap = copy;
        }
        Console.WriteLine($"{Name} выполенна.");
        bitmap.NotifyPixelsChanged();
    }
}