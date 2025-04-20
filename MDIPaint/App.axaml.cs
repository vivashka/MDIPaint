using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MDIPaint.ViewModels;
using MDIPaint.ViewModels.Contracts;
using MDIPaint.ViewModels.PainCanvas;
using MDIPaint.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MDIPaint;

public partial class App : Application
{
    private IServiceProvider _services;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Создаем коллекцию сервисов
        var services = new ServiceCollection();
        
        // Регистрируем сервисы
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
       
        services.AddTransient<MainWindowViewModel>();
        
        _services = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            
            // Получаем Canvas после создания окна
            var mainViewModel = _services.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = mainViewModel;
        
            // Настраиваем DialogService
            var dialogService = _services.GetRequiredService<IDialogService>() as AvaloniaDialogService;
            dialogService?.SetOwner(mainWindow);
        
            mainWindow.DataContext = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
            this.AttachDevTools();
        }
       
        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}