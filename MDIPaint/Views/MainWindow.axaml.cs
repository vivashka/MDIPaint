using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Interactivity;
using Avalonia.Media;
using MDIPaint.Models.Enum;


namespace MDIPaint.Views;

public partial class MainWindow : Window
{
    private int _windowCounter = 0;

    private Color _mainColor = Colors.White;

    public int DrawThickness = 1;

    private SolidColorBrush currentBrush;
    private ShapeType currentShape;
    private double currentSize;

    public Color MainColor
    {
        get => _mainColor;
        set
        {
            if (_mainColor != value)
            {
                _mainColor = value;
                var prev = this.FindControl<ColorPicker>("PreviewColor");
                prev.Color = value;
            }
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        currentBrush = new SolidColorBrush(Colors.White);
        currentShape = ShapeType.Polyline;
        currentSize = 1;
    }

    private void AboutProgram_OnClick(object? sender, RoutedEventArgs e)
    {
        var additionalInformation = new AboutAvaloniaDialog
        {
            Icon = new WindowIcon("C:\\Users\\vovac\\RiderProjects\\MDIPaint\\MDIPaint\\wwwroot\\ColorBrush.png")
        };

        additionalInformation.ShowDialog(this);
    }
}