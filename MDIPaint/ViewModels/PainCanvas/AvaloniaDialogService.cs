using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using MDIPaint.ViewModels.Contracts;

namespace MDIPaint.ViewModels.PainCanvas;

public class AvaloniaDialogService : IDialogService
{
    private Window? _owner;

    public void SetOwner(Window owner) => _owner = owner;

    public async Task<string?> ShowSaveDialogAsync(string title, string defaultExt, FileDialogFilter[] filters)
    {
        if (_owner == null) return null;
        
        var dialog = new SaveFileDialog
        {
            Title = title,
            DefaultExtension = defaultExt,
            Filters = new List<FileDialogFilter>(filters)
        };
        
        return await dialog.ShowAsync(_owner);
    }
    public async Task<string[]?> ShowLoadDialogAsync(string title, FileDialogFilter[] filters)
    {
        if (_owner == null) return null;
        
        var dialog = new OpenFileDialog()
        {
            Title = title, 
            Filters = new List<FileDialogFilter>(filters)
        };
        
        return await dialog.ShowAsync(_owner);
    }
}