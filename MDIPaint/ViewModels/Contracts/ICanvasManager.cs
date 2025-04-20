using Avalonia.Controls;
using MDIPaint.Views;

namespace MDIPaint.ViewModels.Contracts;

public interface ICanvasManager
{
    LayoutWindow? GetActiveLayout();

    void Initialize(Canvas canvas);
}