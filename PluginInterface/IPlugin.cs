using SkiaSharp;

namespace PluginInterface;

public interface IPlugin
{
    string Name { get; }
    string Author { get; }
    void Transform(SKBitmap app);
}
