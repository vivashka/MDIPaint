using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using MDIPaint.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace MDIPaint.Views;

public partial class LoadingModal : ReactiveWindow<LoadingModalViewModel>
{
   
    
    public LoadingModal()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}