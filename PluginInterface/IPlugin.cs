using SkiaSharp;

namespace PluginInterface;

public interface IPlugin
{
    string Name { get; }
    string Author { get; }
    Task Transform(SKBitmap app, IProgress<int>? progress, CancellationToken cancellationToken);
}
