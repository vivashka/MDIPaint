using PluginInterface;
using SkiaSharp;

namespace BWPlugin;

public class BwPlugin : IPlugin
{
    public string Name { get; } = "Black&White";
    public string Author { get; } = "Vladimir";
    public void Transform(SKBitmap app)
    {
        using (var original = app)
        using (var target = new SKBitmap(original.Width, original.Height))
        using (var paint = new SKPaint())
        {
            float threshold = 0.5f;
            paint.ColorFilter = SKColorFilter.CreateBlendMode(SKColors.Black, SKBlendMode.SrcIn);
            app.
        }
        throw new NotImplementedException();
    }
}