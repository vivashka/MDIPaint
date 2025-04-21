using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using MDIPaint.Models.Enum;
using MDIPaint.Views;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace MDIPaint.ViewModels.PainCanvas;

public class PaintCanvas : ContentControl
{
    private Canvas _innerCanvas;
    private Image _mainImage;
    private bool _isDrawing;
    private Point _lastPoint;
    private Point _startPoint;
    private SKBitmap _bitmap;

    private SKCanvas _skCanvas;
    private readonly List<SKPoint> _currentPoints = new();
    private Ellipse _eraserCursor;

    public double ScaleFactor { get; set; } = 1.0;
    private const double MinScale = 0.1;
    private const double MaxScale = 4.0;
    private const double ScaleStep = 0.2;

    private TextItem _currentTextItem;
    private readonly List<TextItem> _textItems = new();
    [Reactive] public SKTypeface TextFont { get; set; } = SKTypeface.Default;


    public bool isFill { get; set; }

    private SKBitmap _previewBitmap;

    public string PathToFile { get; set; }

    public ShapeType CurrentShape { get; set; } = ShapeType.Polyline;
    public SKColor CurrentColor { get; set; } = SKColors.Black;

    public double BrushSize
    {
        get => _brushSize;
        set
        {
            _brushSize = value;
            UpdateEraserSize();
        }
    }

    private double _brushSize = 2;

    public static readonly StyledProperty<double> FixedWidthProperty =
        AvaloniaProperty.Register<PaintCanvas, double>(nameof(FixedWidth), 600);

    public static readonly StyledProperty<double> FixedHeightProperty =
        AvaloniaProperty.Register<PaintCanvas, double>(nameof(FixedHeight), 400);

    public double FixedWidth
    {
        get => GetValue(FixedWidthProperty);
        set => SetValue(FixedWidthProperty, value);
    }

    public double FixedHeight
    {
        get => GetValue(FixedHeightProperty);
        set => SetValue(FixedHeightProperty, value);
    }

    public PaintCanvas(double width, double height)
    {
        FixedWidth = width;
        FixedHeight = height;

        InitializeComponents();
        InitializeBitmap((int)width, (int)height);
        SetupInputHandlers();
    }

    private void InitializeComponents()
    {
        _innerCanvas = new Canvas
        {
            Width = FixedWidth,
            Height = FixedHeight,
            Background = Brushes.White
        };

        _mainImage = new Image
        {
            Width = FixedWidth,
            Height = FixedHeight
        };
        _innerCanvas.Children.Add(_mainImage);

        InitializeEraserCursor();
        _innerCanvas.Children.Add(_eraserCursor);

        Content = _innerCanvas;
    }

    private void InitializeEraserCursor()
    {
        _eraserCursor = new Ellipse
        {
            Width = BrushSize * 2,
            Height = BrushSize * 2,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Fill = Brushes.White,
            IsVisible = false
        };
    }

    public void UpdateEraserSize()
    {
        _eraserCursor.Width = BrushSize * 3;
        _eraserCursor.Height = BrushSize * 3;
    }

    private void InitializeBitmap(int width, int height)
    {
        _bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _previewBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skCanvas = new SKCanvas(_bitmap);
        _skCanvas.Clear(SKColors.Transparent);
        UpdateImageSource();
    }

    private void SetupInputHandlers()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    private void OnPointerEntered(object sender, PointerEventArgs e)
    {
        if (CurrentShape == ShapeType.Eraser)
            _eraserCursor.IsVisible = true;
    }

    private void OnPointerExited(object sender, PointerEventArgs e)
    {
        _eraserCursor.IsVisible = false;
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(_innerCanvas);
        _isDrawing = true;
        _lastPoint = position;
        _startPoint = position;
        _currentPoints.Clear();
        switch (CurrentShape)
        {
            case ShapeType.Polyline:
                DrawFreehand(position);
                break;
            case ShapeType.Ellipse:
                DrawPreviewShape(position);
                break;
            case ShapeType.Eraser:
                ErasePixels(position);
                break;
            case ShapeType.Line:
                DrawPreviewShapeLine(position);
                break;
            case ShapeType.RightArrow:
                DrawPreviewArrow(position);
                break;
            case ShapeType.Text:
                StartTextInput(position);
                break;
            case ShapeType.Bucket:
                PerformFloodFill(position.ToSKPoint());
                break;
        }
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        var position = e.GetPosition(_innerCanvas);
        
        UpdateEraserCursor(position);

        if (!_isDrawing) return;

        switch (CurrentShape)
        {
            case ShapeType.Polyline:
                DrawFreehand(position);
                break;
            case ShapeType.Ellipse:
                DrawPreviewShape(position);
                break;
            case ShapeType.Eraser:
                ErasePixels(position);
                break;
            case ShapeType.Line:
                DrawPreviewShapeLine(position);
                break;
            case ShapeType.RightArrow:
                DrawPreviewArrow(position);
                break;
        }
        _lastPoint = position;
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        _isDrawing = false;
        FinalizeDrawing();
        UpdateImageSource();
    }

    private void DrawFreehand(Point position)
    {
        using var paint = CreatePaint();
        _skCanvas.DrawLine(_lastPoint.ToSKPoint(), position.ToSKPoint(), paint);
        UpdateImageSource();
    }

    private void DrawPreviewShape(Point position)
    {
        // Копируем основное изображение в превью
        using (var canvas = new SKCanvas(_previewBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(_bitmap, 0, 0);

            // Рисуем временную фигуру
            using var paint = CreatePaint();
            canvas.DrawOval(CreateRect(_startPoint, position), paint);
            _lastPoint = position;
        }

        // Объединяем основное изображение и превью
        using (var mergedBitmap = new SKBitmap(_bitmap.Width, _bitmap.Height))
        using (var mergedCanvas = new SKCanvas(mergedBitmap))
        {
            mergedCanvas.Clear(SKColors.Transparent);
            mergedCanvas.DrawBitmap(_bitmap, 0, 0);
            mergedCanvas.DrawBitmap(_previewBitmap, 0, 0);
            UpdateTempImageSource(mergedBitmap);
        }
    }

    private void DrawPreviewShapeLine(Point position)
    {
        // Копируем основное изображение в превью
        using (var canvas = new SKCanvas(_previewBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(_bitmap, 0, 0);

            // Рисуем временную фигуру
            using var paint = CreatePaint();
            canvas.DrawLine(new SKPoint((float)_startPoint.X, (float)_startPoint.Y),
                new SKPoint((float)position.X, (float)position.Y),
                paint);

            _lastPoint = position;
        }

        // Объединяем основное изображение и превью
        using (var mergedBitmap = new SKBitmap(_bitmap.Width, _bitmap.Height))
        using (var mergedCanvas = new SKCanvas(mergedBitmap))
        {
            mergedCanvas.Clear(SKColors.Transparent);
            mergedCanvas.DrawBitmap(_bitmap, 0, 0);
            mergedCanvas.DrawBitmap(_previewBitmap, 0, 0);
            UpdateTempImageSource(mergedBitmap);
        }
    }

    private SKPaint CreatePaint()
    {
        return new SKPaint
        {
            Style = isFill ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            Color = CurrentColor,
            StrokeWidth = (float)BrushSize,
            IsAntialias = true,
            BlendMode = CurrentShape == ShapeType.Eraser ? SKBlendMode.Clear : SKBlendMode.SrcOver,
        };
    }

    private SKRect CreateRect(Point start, Point end)
    {
        return new SKRect(
            (float)Math.Min(start.X, end.X),
            (float)Math.Min(start.Y, end.Y),
            (float)Math.Max(start.X, end.X),
            (float)Math.Max(start.Y, end.Y));
    }

    public void UpdateEraserCursor(Point position)
    {
        if (CurrentShape == ShapeType.Eraser)
        {
            _eraserCursor.IsVisible = true;
            Canvas.SetLeft(_eraserCursor, position.X - _eraserCursor.Width / 2);
            Canvas.SetTop(_eraserCursor, position.Y - _eraserCursor.Height / 2);
        }
        else
        {
            _eraserCursor.IsVisible = false;
        }
    }

    private void ErasePixels(Point position)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.StrokeAndFill,
            Color = SKColors.Transparent,
            BlendMode = SKBlendMode.Clear,
            StrokeWidth = (float)BrushSize,
        };
        _skCanvas.DrawCircle(position.ToSKPoint(), (float)BrushSize, paint);
        UpdateImageSource();
    }

    private void FinalizeDrawing()
    {
        using var paint = CreatePaint();

        switch (CurrentShape)
        {
            case ShapeType.Ellipse:
                // Рисуем финальную версию прямо в основном битмапе
                _skCanvas.DrawOval(CreateRect(_startPoint, _lastPoint), paint);
                break;
            case ShapeType.Line:
                // Рисуем финальную версию прямо в основном битмапе
                _skCanvas.DrawLine(new SKPoint((float)_startPoint.X, (float)_startPoint.Y),
                    new SKPoint((float)_lastPoint.X, (float)_lastPoint.Y),
                    paint);
                break;
            case ShapeType.RightArrow:
                // Рисуем финальную стрелку на основном холсте
                DrawHollowArrow(_skCanvas, _startPoint.ToSKPoint(), _lastPoint.ToSKPoint(), paint);
                break;
        }

        UpdateImageSource();
    }

    private void UpdateImageSource()
    {
        using var image = SKImage.FromBitmap(_bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        _mainImage.Source = new Bitmap(new MemoryStream(data.ToArray()));
    }

    private void UpdateTempImageSource(SKBitmap tempBitmap)
    {
        using var image = SKImage.FromBitmap(tempBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        _mainImage.Source = new Bitmap(new MemoryStream(data.ToArray()));
    }

    public void SaveCanvas(string filePath)
    {
        PathToFile = filePath;
        using (var saveBitmap = new SKBitmap(_bitmap.Width, _bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul))
        using (var saveCanvas = new SKCanvas(saveBitmap))
        {
            // Заливаем белым цветом
            saveCanvas.Clear(SKColors.White);

            // Копируем содержимое основного холста
            saveCanvas.DrawBitmap(_bitmap, 0, 0);

            // Сохраняем результат
            using (var image = SKImage.FromBitmap(saveBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }
        }
    }

    public void LoadImage(string filePath)
    {
        PathToFile = filePath;
        using var stream = File.OpenRead(filePath);
        using var newBitmap = SKBitmap.Decode(stream);

        // Обновляем размеры
        FixedWidth = newBitmap.Width;
        FixedHeight = newBitmap.Height;

        // Пересоздаем битмап
        _bitmap = newBitmap.Copy();

        // Обновляем размеры компонентов
        _innerCanvas.Width = FixedWidth;
        _innerCanvas.Height = FixedHeight;
        _mainImage.Width = FixedWidth;
        _mainImage.Height = FixedHeight;

        InitializeComponents();

        using var image = SKImage.FromBitmap(newBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        _mainImage.Source = new Bitmap(new MemoryStream(data.ToArray()));

        _previewBitmap = new SKBitmap((int)FixedWidth, (int)FixedHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skCanvas = new SKCanvas(_bitmap);
        UpdateImageSource();

        SetupInputHandlers();
    }

    public void ZoomIn()
    {
        ScaleFactor = Math.Min(MaxScale, ScaleFactor + ScaleStep);
        UpdateCanvasSize();
    }

    public void ZoomOut()
    {
        ScaleFactor = Math.Max(MinScale, ScaleFactor - ScaleStep);
        UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        FixedWidth = _bitmap.Width * ScaleFactor;
        FixedHeight = _bitmap.Height * ScaleFactor;

        _innerCanvas.Width = FixedWidth;
        _innerCanvas.Height = FixedHeight;
        _mainImage.Width = FixedWidth;
        _mainImage.Height = FixedHeight;
    }


    private async void StartTextInput(Point position)
    {
        var dialog = new TextInputDialog();
        var parent = this.Parent;
        Window temp = null;
        while (parent != null)
        {
            if (parent is Window window)
            {
                temp = window;
            }

            parent = parent.Parent;
        }

        if (await dialog.ShowDialog<bool>(temp))
        {
            _currentTextItem = new TextItem
            {
                Text = dialog.Text,
                Position = position.ToSKPoint(),
                Color = CurrentColor,
                Size = (float)BrushSize,
                Typeface = TextFont
            };

            _textItems.Add(_currentTextItem);
            RedrawCanvas();
        }
    }

    private void RedrawCanvas()
    {
        using (var canvas = new SKCanvas(_bitmap))
        {
            // Рисуем все элементы
            foreach (var textItem in _textItems)
            {
                DrawTextItem(canvas, textItem);
            }
        }

        UpdateImageSource();
    }

    private void DrawTextItem(SKCanvas canvas, TextItem item)
    {
        using var paint = new SKPaint
        {
            Color = item.Color,
            TextSize = item.Size,
            Typeface = item.Typeface,
            IsAntialias = true, TextAlign = SKTextAlign.Center
        };

        canvas.DrawText(item.Text, item.Position, paint);
    }

    private unsafe void PerformFloodFill(SKPoint startPoint)
    {
        // Получаем целочисленные координаты
        int x = (int)startPoint.X;
        int y = (int)startPoint.Y;

        // Проверка границ
        if (x < 0 || x >= _bitmap.Width || y < 0 || y >= _bitmap.Height)
            return;

        // Получаем целевой цвет
        SKColor targetColor = _bitmap.GetPixel(x, y);
        SKColor fillColor = CurrentColor;

        // Если цвет совпадает - выходим
        if (targetColor == fillColor)
            return;

        // Блокируем битмап для прямого доступа
        using var pixmap = _bitmap.PeekPixels();
        var info = pixmap.Info;
        byte* ptr = (byte*)pixmap.GetPixels();

        // Создаем очередь и добавляем начальную точку
        var queue = new Queue<(int, int)>();
        queue.Enqueue((x, y));

        // Массив направлений (4-связная область)
        var directions = new (int, int)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        while (queue.Count > 0)
        {
            var (currentX, currentY) = queue.Dequeue();

            // Проверка границ
            if (currentX < 0 || currentX >= info.Width || currentY < 0 || currentY >= info.Height)
                continue;

            // Получаем цвет текущего пикселя
            var offset = currentY * info.RowBytes + currentX * 4;
            var currentColor = new SKColor(ptr[offset + 2], ptr[offset + 1], ptr[offset], ptr[offset + 3]);

            // Сравнение цветов с допуском
            if (!ColorsAreSimilar(currentColor, targetColor, tolerance: 10))
                continue;

            // Закрашиваем пиксель
            ptr[offset] = fillColor.Blue;
            ptr[offset + 1] = fillColor.Green;
            ptr[offset + 2] = fillColor.Red;
            ptr[offset + 3] = fillColor.Alpha;

            // Добавляем соседей
            foreach (var (dx, dy) in directions)
            {
                queue.Enqueue((currentX + dx, currentY + dy));
            }
        }

        UpdateImageSource();
    }

    private bool ColorsAreSimilar(SKColor a, SKColor b, int tolerance)
    {
        return Math.Abs(a.Red - b.Red) <= tolerance &&
               Math.Abs(a.Green - b.Green) <= tolerance &&
               Math.Abs(a.Blue - b.Blue) <= tolerance &&
               Math.Abs(a.Alpha - b.Alpha) <= tolerance;
    }

    private void DrawPreviewArrow(Point position)
    {
        using (var canvas = new SKCanvas(_previewBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(_bitmap, 0, 0);
            using var paint = CreatePaint();
            DrawHollowArrow(canvas, _startPoint.ToSKPoint(), position.ToSKPoint(), paint);
        }

        UpdateTempImageSource(_previewBitmap);
    }

    private void DrawHollowArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
    {
        float arrowHeadSize = 20f; // Длина наконечника стрелки
        float arrowBaseWidth = 14f; // Ширина основания головки
        float arrowBodyWidth = 8f; // Ширина тела стрелки (меньше основания)

        using (var path = new SKPath())
        {
            // Вектор направления стрелки
            SKPoint direction = new SKPoint(end.X - start.X, end.Y - start.Y);
            float length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length == 0) return; // Предотвращаем деление на ноль

            // Нормализация вектора
            SKPoint unitDir = new SKPoint(direction.X / length, direction.Y / length);
            SKPoint normal = new SKPoint(-unitDir.Y, unitDir.X); // Перпендикулярный вектор

            // Прямоугольное тело стрелки (уже, чем основание)
            SKPoint bodyStart1 = new SKPoint(start.X + normal.X * arrowBodyWidth, start.Y + normal.Y * arrowBodyWidth);
            SKPoint bodyStart2 = new SKPoint(start.X - normal.X * arrowBodyWidth, start.Y - normal.Y * arrowBodyWidth);
            SKPoint bodyEnd1 = new SKPoint(end.X - unitDir.X * arrowHeadSize + normal.X * arrowBodyWidth,
                end.Y - unitDir.Y * arrowHeadSize + normal.Y * arrowBodyWidth);
            SKPoint bodyEnd2 = new SKPoint(end.X - unitDir.X * arrowHeadSize - normal.X * arrowBodyWidth,
                end.Y - unitDir.Y * arrowHeadSize - normal.Y * arrowBodyWidth);

            // Основание головки (шире тела)
            SKPoint arrowBase1 = new SKPoint(end.X - unitDir.X * arrowHeadSize + normal.X * arrowBaseWidth,
                end.Y - unitDir.Y * arrowHeadSize + normal.Y * arrowBaseWidth);
            SKPoint arrowBase2 = new SKPoint(end.X - unitDir.X * arrowHeadSize - normal.X * arrowBaseWidth,
                end.Y - unitDir.Y * arrowHeadSize - normal.Y * arrowBaseWidth);

            // Вершина головки стрелки
            SKPoint arrowTip = new SKPoint(end.X, end.Y);

            // Создание пути
            path.MoveTo(bodyStart1);
            path.LineTo(bodyEnd1);
            path.LineTo(arrowBase1);
            path.LineTo(arrowTip);
            path.LineTo(arrowBase2);
            path.LineTo(bodyEnd2);
            path.LineTo(bodyStart2);
            path.Close();

            // Отрисовка контура стрелки
            canvas.DrawPath(path, paint);
        }
    }
}