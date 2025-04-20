using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MDIPaint.Views;

public partial class TextInputDialog : Window
{
    public string Text { get; private set; }
    
    public TextInputDialog()
    {
        InitializeComponent();
    }
    
    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Text = InputBox.Text;
        Close(true);
    }
}