using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MDIPaint.ViewModels.PainCanvas;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MDIPaint.ViewModels;

public partial class LayoutWindowViewModel : ReactiveObject
{

    [Reactive] public Cursor PaintCursor { get; set; } = new Cursor(StandardCursorType.Cross);
    
    
    public PaintCanvas Layout { get; set; }
    
    [Reactive] public IBrush BorderColor { get; set; } = Brushes.DimGray;
    

    public LayoutWindowViewModel()
    {
        Layout = new PaintCanvas(600, 400);
    }
}