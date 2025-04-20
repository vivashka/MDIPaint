using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MDIPaint.Models.Enum;
using MDIPaint.ViewModels.Contracts;
using MDIPaint.ViewModels.PainCanvas;
using MDIPaint.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;

namespace MDIPaint.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    private byte _sliderValue = 1;
    
    [Reactive] public double EraserRadius { get; set; } = 20;

    [Reactive] public ShapeType Shape { get; set; } = ShapeType.Polyline;
    
    [Reactive] public bool isFill { get; set; } = false;

    public byte SliderValue
    {
        get => _sliderValue;
        set { this.RaiseAndSetIfChanged(ref _sliderValue, value); }
    }
    

    [Reactive] public Canvas MainCanvas { get; set; }

    [Reactive] public ColorPicker ColorPalette { get; set; }


    private readonly IDialogService _dialogService;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public MainWindowViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        ColorPalette = new ColorPicker();

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);

        MainCanvas = new Canvas()
        {
            Background = Brushes.LightGray, Name = "MainCanvas"
        };
        
        

        this.WhenAnyValue(p_vm => p_vm.SliderValue).Subscribe(_ => ChangeBrushSize());
        this.WhenAnyValue(p_vm => p_vm.ColorPalette.Color).Subscribe(_ => ChangeBrushColor());
        this.WhenAnyValue(p_vm => p_vm.Shape).Subscribe(_ => ChangeShape());
        this.WhenAnyValue(p_vm => p_vm.MainCanvas).Subscribe(_ => ChangeMainLayout());
        this.WhenAnyValue(p_vm => p_vm.isFill).Subscribe(_ => ChangeFillState());
    }

    public void ChangeFillState()
    {
        if (MainCanvas!.Children.Count > 0)
        {
            foreach (var child in MainCanvas!.Children!.OfType<LayoutWindow>())
            {
                child.PaintArea.Layout.isFill = isFill;
            }
        }
    }

    public void ChangeMainLayout()
    {
        if (MainCanvas!.Children.Count > 0)
        {
            int maxChild = MainCanvas!.Children!.OfType<LayoutWindow>()!.MaxBy(ch => ch.ZIndex)!.ZIndex!;
        
            foreach (var child in MainCanvas!.Children!.OfType<LayoutWindow>())
            {
                if (child.ZIndex == maxChild)
                {
                    child.PaintArea.BorderColor = Brushes.Aqua;
                }
                else
                {
                    child.PaintArea.BorderColor = Brushes.Gray;
                }
            }
        }
        
        
    }

    private void ChangeShape()
    {
        
        foreach (var item in MainCanvas.Children)
        {
            if (item.DataContext is LayoutWindowViewModel lwv)
            {
                lwv.Layout.CurrentShape = Shape;
                
                lwv.PaintCursor = new Cursor(StandardCursorType.Cross);
                if (Shape == ShapeType.Eraser)
                {
                    lwv.PaintCursor = new Cursor(StandardCursorType.No);
                }
                else if (Shape == ShapeType.Text)
                {
                    lwv.PaintCursor = new Cursor(StandardCursorType.Ibeam);
                }
                else if (Shape == ShapeType.Bucket)
                {
                    lwv.PaintCursor = new Cursor(StandardCursorType.DragCopy);
                }
                
            }
        }
    }
    

    private void ChangeBrushSize()
    {
        foreach (var item in MainCanvas.Children)
        {
            if (item.DataContext is LayoutWindowViewModel lwv)
            {
                lwv.Layout.BrushSize = SliderValue;
                lwv.Layout.UpdateEraserSize();
            }
        }
    }

    private void ChangeBrushColor()
    {
        foreach (var item in MainCanvas.Children)
        {
            if (item.DataContext is LayoutWindowViewModel lwv)
            {
                lwv.Layout.CurrentColor = new SKColor(ColorPalette.Color.ToUInt32());
            }
        }
    }

    public void Green_OnClick()
    {
        ColorPalette.Color = Colors.Green;
    }

    public void Red_OnClick()
    {
        ColorPalette.Color = Colors.Red;
    }

    public void Blue_OnClick()
    {
        ColorPalette.Color = Colors.Blue;
    }

    public void CreateNewLayout()
    {
        LayoutWindow innerWindowView = new LayoutWindow();
        innerWindowView.PaintArea.Layout.CurrentColor = new SKColor(ColorPalette.Color.ToUInt32());;
        innerWindowView.PaintArea.Layout.BrushSize = SliderValue;
        innerWindowView.PaintArea.Layout.CurrentShape = Shape;
        innerWindowView.PaintArea.Layout.isFill = isFill;
        Canvas.SetLeft(innerWindowView, MainCanvas.Children.Count * 30);
        Canvas.SetTop(innerWindowView, MainCanvas.Children.Count * 30);
        if (MainCanvas!.Children.Count > 0)
        {
            innerWindowView.ZIndex = MainCanvas!.Children!.OfType<LayoutWindow>()!.MaxBy(ch => ch!.ZIndex)!.ZIndex + 1;
        }
        
        MainCanvas.Children.Add(innerWindowView);
    }

    [Obsolete("Obsolete")]
    public async Task SaveAsync()
    {
        var filters = new[]
        {
            new FileDialogFilter { Name = "BMP Image", Extensions = { "bmp" } },
            new FileDialogFilter { Name = "JPEG Image", Extensions = { "jpg", "jpeg" } },
            new FileDialogFilter { Name = "PNG Image", Extensions = { "png" } }
        };

        var path = await _dialogService.ShowSaveDialogAsync(
            "Сохранить изображение",
            "png",
            filters
        );
        if (string.IsNullOrEmpty(path)) return;

        var layout = MainCanvas.Children.OfType<LayoutWindow>().MaxBy(w => w.ZIndex)!
            .PaintArea.Layout;
        
        if (layout != null)
        {
            layout.SaveCanvas(path);
        }
    }
    
    [Obsolete("Obsolete")]
    public async Task SaveConditionalAsync()
    {
        var layout = MainCanvas.Children.OfType<LayoutWindow>().MaxBy(w => w.ZIndex)!
            .PaintArea.Layout;
        if (!string.IsNullOrWhiteSpace(layout.PathToFile))
        {
            if (layout != null)
            {
                layout.SaveCanvas(layout.PathToFile);
            }
        }
        else
        {
            var filters = new[]
            {
                new FileDialogFilter { Name = "BMP Image", Extensions = { "bmp" } },
                new FileDialogFilter { Name = "JPEG Image", Extensions = { "jpg", "jpeg" } },
                new FileDialogFilter { Name = "PNG Image", Extensions = { "png" } }
            };

            var path = await _dialogService.ShowSaveDialogAsync(
                "Сохранить изображение",
                "png",
                filters
            );
            if (string.IsNullOrEmpty(path)) return;
        
            if (layout != null)
            {
                layout.SaveCanvas(path);
            }
        }
        
    }

    public async Task LoadAsync()
    {
        var filters = new[]
        {
            new FileDialogFilter { Name = "PNG Image", Extensions = { "png", "jpg", "jpeg", "bmp" } }
        };

        var path = await _dialogService.ShowLoadDialogAsync(
            "Сохранить изображение",
            filters
        );

        if (path.Length > 0)
            foreach (var pathString in path)
            {
                LayoutWindow innerWindowView = new LayoutWindow();
                innerWindowView.PaintArea.Layout.CurrentColor = new SKColor(ColorPalette.Color.ToUInt32());;
                innerWindowView.PaintArea.Layout.isFill = isFill;
                innerWindowView.PaintArea.Layout.CurrentShape = Shape;
                innerWindowView.PaintArea.Layout.BrushSize = SliderValue;

                Canvas.SetLeft(innerWindowView, 20 + MainCanvas.Children.Count * 30);
                Canvas.SetTop(innerWindowView, 20 + MainCanvas.Children.Count * 30);
                innerWindowView.PaintArea.Layout.LoadImage(pathString);
                MainCanvas.Children.Add(innerWindowView);
            }
    }

    public void OnPositionCascade()
    {
        int offset = 30;
        for (int i = 0; i < MainCanvas.Children.Count; i++)
        {
            MainCanvas.Children[i].ZIndex = i;
            MainCanvas.Children[i].Height = 100;
            MainCanvas.Children[i].Width = 200;
            Canvas.SetTop(MainCanvas.Children[i], offset*i);
            Canvas.SetLeft(MainCanvas.Children[i], offset*i);
        }
    }

    public void OnLeftToRightCascade()
    {
        if (MainCanvas.Children.Count == 0) return;
    
        var totalWidth = MainCanvas.Bounds.Width;
        var segmentWidth = totalWidth / MainCanvas.Children.Count;
    
        for (int i = 0; i < MainCanvas.Children.Count; i++)
        {
            var child = MainCanvas.Children[i];
            child.ZIndex = i;
            Canvas.SetLeft(child, segmentWidth * i);
            Canvas.SetTop(child, 0);
        
            
            child.Width = segmentWidth;
            child.Height = MainCanvas.Bounds.Height;
        }
    }
    
    public void OnTopToBottomCascade()
    {
        if (MainCanvas.Children.Count == 0) return;
    
        var totalHeight = MainCanvas.Bounds.Height;
        var segmentHeight = totalHeight / MainCanvas.Children.Count;
    
        for (int i = 0; i < MainCanvas.Children.Count; i++)
        {
            var child = MainCanvas.Children[i];
            child.ZIndex = i;
            Canvas.SetLeft(child, 0);
            Canvas.SetTop(child, segmentHeight * i);
        
            
            child.Height = segmentHeight;
            child.Width = MainCanvas.Bounds.Width;
        }
    }

    public void OnZoomPlus()
    {
        var layout = MainCanvas.Children.OfType<LayoutWindow>().MaxBy(w => w.ZIndex)!
            .PaintArea.Layout;
        layout.ZoomIn();
    }
    
    public void OnZoomMinus()
    {
        var layout = MainCanvas.Children.OfType<LayoutWindow>().MaxBy(w => w.ZIndex)!
            .PaintArea.Layout;
        layout.ZoomOut();
    }
}




