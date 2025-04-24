using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MDIPaint.Models;
using MDIPaint.Models.Enum;
using MDIPaint.ViewModels.Contracts;
using MDIPaint.Views;
using PluginInterface;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SkiaSharp;
using Splat;

namespace MDIPaint.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    private byte _sliderValue = 1;

    [Reactive] public ShapeType Shape { get; set; } = ShapeType.Polyline;

    [Reactive] public bool isFill { get; set; } = false;

    [Reactive] public Filters Filter { get; set; } = Filters.BW;

    public byte SliderValue
    {
        get => _sliderValue;
        set { this.RaiseAndSetIfChanged(ref _sliderValue, value); }
    }

    [Reactive] public Canvas MainCanvas { get; set; }

    [Reactive] public ColorPicker ColorPalette { get; set; }


    private readonly IDialogService _dialogService;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public readonly Plugins Plugins;

    public MenuItem PluginsItems { get; set; } = new MenuItem();


    public MainWindowViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        ColorPalette = new ColorPicker();

        PluginsItems.Header = "Фильтр";


        PluginsItems.SelectedIndex = (int)Filter;

        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);

        MainCanvas = new Canvas()
        {
            Background = Brushes.LightGray, Name = "MainCanvas"
        };
        MainCanvas.Loaded += (sender, args) => FindPlugins();

        Plugins = Locator.GetLocator().GetService<Plugins>();
        this.WhenAnyValue(p_vm => p_vm.SliderValue).Subscribe(_ => ChangeBrushSize());
        this.WhenAnyValue(p_vm => p_vm.ColorPalette.Color).Subscribe(_ => ChangeBrushColor());
        this.WhenAnyValue(p_vm => p_vm.Shape).Subscribe(_ => ChangeShape());
        this.WhenAnyValue(p_vm => p_vm.MainCanvas).Subscribe(_ => ChangeMainLayout());
        this.WhenAnyValue(p_vm => p_vm.isFill).Subscribe(_ => ChangeFillState());
    }

    void FindPlugins()
    {
        // папка с плагинами
        string folder = AppDomain.CurrentDomain.BaseDirectory;

        // dll-файлы в этой папке
        string[] files = Directory.GetFiles(folder, "*.dll");

        foreach (string file in files)
        {
            try
            {
                Assembly assembly = Assembly.LoadFile(file);

                foreach (Type type in assembly.GetTypes())
                {
                    Type iface = type.GetInterface("PluginInterface.IPlugin");

                    if (iface != null)
                    {
                        IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                        if (!Plugins.Filters.ContainsKey(plugin.Name))
                        {
                            Plugins.Filters[plugin.Name] = true;
                        }

                        var item = new MenuItem
                        {
                            Header = plugin.Name,
                            IsVisible = Plugins.Filters[plugin.Name]
                        };
                        item.Header = plugin.Name;
                        item.Command = ReactiveCommand.Create(() =>
                        {
                            OnSetPlugin(plugin);
                            PluginsItems.Close();
                        });
                        PluginsItems.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка {ex}");
            }
        }

        string pluginsText = "Загруженные плагины:\n";

        byte counter = 1;
        foreach (var (key, isActive) in Plugins.Filters)
        {
            if (isActive)
            {
                pluginsText += $"{counter}. {key}\n";
                counter++;
            }
        }

        Plugins.SavePluginsToAppSettings(Plugins);
        InformationModal(pluginsText);
    }

    private async void OnSetPlugin(IPlugin plugin)
    {
        var loading = new LoadingModal
        {
            Width = 300,
            Height = 150
        };
        var layout = MainCanvas.Children.OfType<LayoutWindow>().MaxBy(w => w.ZIndex)!
            .PaintArea.Layout;

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        loading.ViewModel = new LoadingModalViewModel(cancellationTokenSource);
        loading.ViewModel.TitleWindow = plugin.GetType().FullName;
        loading.ViewModel.Text = $"Загрузка {plugin.GetType()}";
        
        var progress = new Progress<int>(p => loading.ViewModel.Progress = p);
        loading.Show();
        
        await layout.SetPluginAsync(plugin.GetType(), progress, cancellationTokenSource);
        loading.Close();
    }

    private async void InformationModal(string text)
    {
        var dialog = new InformationWindow($"{text}", "Дополнительная информация");
        dialog.Width = 300;
        dialog.Height = 150;
        Window parent = (Window)MainCanvas.Parent.Parent.Parent;
        await dialog.ShowDialog(parent);
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
        innerWindowView.PaintArea.Layout.CurrentColor = new SKColor(ColorPalette.Color.ToUInt32());
        ;
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
                innerWindowView.PaintArea.Layout.CurrentColor = new SKColor(ColorPalette.Color.ToUInt32());
                ;
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
            Canvas.SetTop(MainCanvas.Children[i], offset * i);
            Canvas.SetLeft(MainCanvas.Children[i], offset * i);
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

    public async Task SetPlugin(Type filter, Progress<int> progress)
    {
      
        
    }
}