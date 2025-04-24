using System;
using System.Reactive;
using System.Threading;
using MDIPaint.ViewModels.PainCanvas;
using PluginInterface;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Console = System.Console;

namespace MDIPaint.ViewModels;

public class LoadingModalViewModel : ReactiveObject
{
    public string TitleWindow { get; set; } = "Плагин";
    
    public string Text { get; set; } = "Загрузка";
    
    private int _progress = 0;
    public int Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public LoadingModalViewModel(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
        CancelCommand = ReactiveCommand.Create(CancelProgress);
    }

    private void CancelProgress()
    {
        _cancellationTokenSource.Cancel();
    }
}