using System;
using System.Net.Http;
using System.Text.Json;
using SkiaSharp;
using PluginInterface;

namespace AdditionalInformationPlugin
{
    public class AdditionalInformationPlugin : IPlugin
    {
        public string Name => "Data&Geolocation";
        public string Author => "Vladimir";

        private int _delay = 500;

        public async Task Transform(SKBitmap bitmap, IProgress<int>? progress, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(async () =>
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Thread.Sleep(_delay);
                    progress?.Report(10);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }


                    string location = await GetGeoLocationFromIpApi();
                    using var surface =
                        SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height, bitmap.ColorType,
                            bitmap.AlphaType));
                    var canvas = surface.Canvas;

                    canvas.DrawBitmap(bitmap, 0, 0);


                    float textSize = Math.Max(bitmap.Width, bitmap.Height) / 40f;
                    var paintText = new SKPaint
                    {
                        Color = SKColors.White,
                        IsAntialias = true,
                        TextSize = textSize,
                        Typeface = SKTypeface.FromFamilyName("Arial")
                    };
                    Thread.Sleep(_delay);
                    progress?.Report(30);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }

                    var paintBg = new SKPaint
                    {
                        Color = new SKColor(0, 0, 0, 180)
                    };


                    string[] lines = { timestamp, location };
                    float padding = textSize * 0.3f;
                    float lineHeight = paintText.FontMetrics.CapHeight + padding;
                    float bgHeight = lines.Length * lineHeight + padding;
                    float bgWidth = 0;
                    foreach (var line in lines)
                    {
                        bgWidth = Math.Max(bgWidth, paintText.MeasureText(line));
                    }

                    bgWidth += 2 * padding;
                    Thread.Sleep(_delay);
                    progress?.Report(60);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }

                    var margin = padding;
                    var rect = new SKRect(
                        margin,
                        bitmap.Height - margin - bgHeight,
                        margin + bgWidth,
                        bitmap.Height - margin
                    );
                    canvas.DrawRect(rect, paintBg);


                    for (int i = 0; i < lines.Length; i++)
                    {
                        float x = rect.Left + padding;
                        float y = rect.Top + padding + (i + 1) * lineHeight - paintText.FontMetrics.Descent;
                        canvas.DrawText(lines[i], x, y, paintText);
                    }

                    Thread.Sleep(_delay);
                    progress?.Report(90);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }

                    surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.Info.RowBytes, 0, 0);
                    Thread.Sleep(_delay);
                    progress?.Report(100);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }

                    bitmap.NotifyPixelsChanged();
                }, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<string> GetGeoLocationFromIpApi()
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(2)
                };
                string url = "http://ip-api.com/json/";
                var json = await client.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("status").GetString() == "success")
                {
                    double lat = root.GetProperty("lat").GetDouble();
                    double lon = root.GetProperty("lon").GetDouble();
                    return $"{lat:F6}, {lon:F6}";
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return "Unknown location";
        }
    }
}