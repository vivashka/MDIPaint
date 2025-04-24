using Avalonia;
using System;
using System.IO;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Splat;

namespace MDIPaint;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        try
        {
            Console.WriteLine("Start bootstrapper register");
            Bootstrapper.Register(Locator.CurrentMutable, configuration);
            Console.WriteLine("Finish bootstrapper register");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error bootstrapper register\n{error}", ex.ToString());
            return;
        }
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    
}