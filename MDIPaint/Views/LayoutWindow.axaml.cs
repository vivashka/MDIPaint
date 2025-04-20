using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using MDIPaint.Models.Enum;
using MDIPaint.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MDIPaint.Views;

public partial class LayoutWindow : UserControl
{
    private bool _isDragging;
    private Point _dragStartPosition;
    private bool _isResizing;
    private Point _resizeStartPosition;
    private double _initialWidth;
    private double _initialHeight;
    
    private ResizeEdge _resizeEdge;
    
    

    private int _widthTopPanel = 200;

    private bool _isMaximize;
    
    public LayoutWindowViewModel PaintArea { get; set; }
    
    public LayoutWindow()
    {
        InitializeComponent();
        PaintArea = new LayoutWindowViewModel();
        DataContext = PaintArea;
    }
    

    private void Header_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        _isDragging = true;
        _dragStartPosition = e.GetCurrentPoint(this).Position;

    }

    private void Header_PointerMoved(object sender, PointerEventArgs e)
    {
        if (_isDragging && PaintArea.Layout.IsVisible && !_isMaximize)
        {
            Point currentPosition = e.GetCurrentPoint(this).Position;
            double deltaX = currentPosition.X - _dragStartPosition.X;
            double deltaY = currentPosition.Y - _dragStartPosition.Y;
            
            if (Parent is Canvas parentCanvas)
            {
                double currentLeft = Canvas.GetLeft(this);
                double currentTop = Canvas.GetTop(this);

                Canvas.SetLeft(this, currentLeft + deltaX);
                Canvas.SetTop(this, currentTop + deltaY);
            }
        }
    }

    private void Header_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }
    
    private void ResizeBorder_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        Point point = e.GetCurrentPoint(ResizeBorder).Position;
        ResizeEdge edge = GetResizeEdge(point);
        if (edge != ResizeEdge.None)
        {
            _isResizing = true;
            _resizeEdge = edge;
            _resizeStartPosition = point;
            _initialWidth = Width;
            _initialHeight = Height;
            ResizeBorder.Cursor = GetResizeCursor(edge);

        }
    }

    private void ResizeBorder_PointerMoved(object sender, PointerEventArgs e)
    {
        if (_isResizing && PaintArea.Layout.IsVisible && !_isMaximize)
        {
            Point currentPosition = e.GetCurrentPoint(ResizeBorder).Position;
            double deltaX = currentPosition.X - _resizeStartPosition.X;
            double deltaY = currentPosition.Y - _resizeStartPosition.Y;

            double newWidth = Width;
            double newHeight = Height;
            double newLeft = Canvas.GetLeft(this);
            double newTop = Canvas.GetTop(this);
            
            switch (_resizeEdge)
            {
                case ResizeEdge.Left:
                    newLeft = Canvas.GetLeft(this) + deltaX;
                    newWidth -= deltaX;
                    break;
                case ResizeEdge.Right:
                    newWidth = Math.Max(0, _initialWidth + deltaX);
                    break;
                case ResizeEdge.Top:
                    newHeight -= deltaY;
                    newTop = Canvas.GetTop(this) + deltaY;
                    break;
                case ResizeEdge.Bottom:
                    newHeight = Math.Max(0, _initialHeight + deltaY);
                    break;
                case ResizeEdge.TopLeft:
                    newWidth -= deltaX;
                    newLeft = Canvas.GetLeft(this) + deltaX;
                    newHeight -= deltaY;
                    newTop = Canvas.GetTop(this) + deltaY;
                    break;
                case ResizeEdge.TopRight:
                    newWidth = Math.Max(0, _initialWidth + deltaX);
                    newHeight -= deltaY;
                    newTop = Canvas.GetTop(this) + deltaY;
                    break;
                case ResizeEdge.BottomLeft:
                    newWidth -= deltaX;
                    newLeft = Canvas.GetLeft(this) + deltaX;
                    newHeight = Math.Max(0, _initialHeight + deltaY);
                    break;
                case ResizeEdge.BottomRight:
                    newWidth = Math.Max(0, _initialWidth + deltaX);
                    newHeight = Math.Max(0, _initialHeight + deltaY);
                    break;
            }
            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
            Width = newWidth;
            Height = newHeight;

        }
        else
        {
            ResizeEdge edge = GetResizeEdge(e.GetCurrentPoint(ResizeBorder).Position);
            ResizeBorder.Cursor = GetResizeCursor(edge);
        }
    }

    private void ResizeBorder_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_isResizing && PaintArea.Layout.IsVisible && !_isMaximize)
        {
            _isResizing = false;
            ResizeBorder.Cursor = new Cursor(StandardCursorType.Arrow);
        }
    }

    private void CloseButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(this);
        }
    }
    
    private void MinimizeButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (PaintArea.Layout.IsVisible)
        {
            PaintArea.Layout.IsVisible = false;
            Height = TopPanel.Bounds.Height;
            Width = _widthTopPanel;
        }
        else
        {
            PaintArea.Layout.IsVisible = true;
            Height = PaintArea.Layout.FixedHeight;
            Width = PaintArea.Layout.FixedWidth;
        }
       
    }
    
    private void MaximizeButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Parent is Panel parentPanel)
        {
            if (_isMaximize)
            {
                Height = PaintArea.Layout.FixedHeight;
                Width = PaintArea.Layout.FixedWidth;
                _isMaximize = false;
            }
            else
            {
                Height = parentPanel.Bounds.Height;
                Width = parentPanel.Bounds.Width;
                Canvas.SetTop(this,0);
                Canvas.SetLeft(this,0);
                _isMaximize = true;
            }
           
        }
        PaintArea.Layout.IsVisible = true;
        
    }

    private ResizeEdge GetResizeEdge(Point point)
    {
        double borderThickness = ResizeBorder.BorderThickness.Top;
        double x = point.X;
        double y = point.Y;
        double width = ResizeBorder.Bounds.Width;
        double height = ResizeBorder.Bounds.Height;

        bool isLeftEdge = x >= 0 && x <= borderThickness;
        bool isRightEdge = x >= width - borderThickness && x <= width;
        bool isTopEdge = y >= 0 && y <= borderThickness;
        bool isBottomEdge = y >= height - borderThickness && y <= height;

        if (isTopEdge && isLeftEdge) return ResizeEdge.TopLeft;
        if (isTopEdge && isRightEdge) return ResizeEdge.TopRight;
        if (isBottomEdge && isLeftEdge) return ResizeEdge.BottomLeft;
        if (isBottomEdge && isRightEdge) return ResizeEdge.BottomRight;
        if (isLeftEdge) return ResizeEdge.Left;
        if (isRightEdge) return ResizeEdge.Right;
        if (isTopEdge) return ResizeEdge.Top;
        if (isBottomEdge) return ResizeEdge.Bottom;

        return ResizeEdge.None;
    }

    private Cursor GetResizeCursor(ResizeEdge edge)
    {
        switch (edge)
        {
            case ResizeEdge.Left:
                return new Cursor(StandardCursorType.LeftSide);
            case ResizeEdge.Right:
                return new Cursor(StandardCursorType.SizeWestEast);
            case ResizeEdge.Top:
                return new Cursor(StandardCursorType.TopSide);
            case ResizeEdge.Bottom:
                return new Cursor(StandardCursorType.SizeNorthSouth);
            case ResizeEdge.TopLeft:
                return new Cursor(StandardCursorType.TopLeftCorner);
            case ResizeEdge.BottomRight:
                return new Cursor(StandardCursorType.BottomRightCorner);
            case ResizeEdge.TopRight:
                return new Cursor(StandardCursorType.TopRightCorner);
            case ResizeEdge.BottomLeft:
                return new Cursor(StandardCursorType.BottomLeftCorner);
            default:
                return new Cursor(StandardCursorType.Hand);
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Parent is Canvas parentCanvas)
        {
            int maxZIndex = 0;
            foreach (var child in parentCanvas.Children)
            {
                if (child is LayoutWindow layoutWindow)
                {
                    int zIndex = layoutWindow.GetValue(Canvas.ZIndexProperty);
                    maxZIndex = Math.Max(maxZIndex, zIndex);
                }
            }
            PaintArea.BorderColor = Brushes.Aqua;
            SetValue(Canvas.ZIndexProperty, maxZIndex + 1);
        }
        else
        {
            if (Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(this);
                parentPanel.Children.Add(this);
            }
        }
    }
}