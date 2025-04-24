using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI.Fody.Helpers;

namespace MDIPaint.Views;

public partial class InformationWindow : Window
{
    public string Text { get; set; }
    
    public string TitleWindow { get; set; }
    
    public InformationWindow(string text, string title)
    {
        DataContext = this;
        Text = text;
        TitleWindow = title;
        InitializeComponent();
    }
}