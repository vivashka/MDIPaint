using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace MDIPaint.ViewModels.Contracts;

public interface IDialogService
{
    [Obsolete("Obsolete")]
    Task<string?> ShowSaveDialogAsync(string title, string defaultExt, FileDialogFilter[] filters);
    
    Task<string[]?> ShowLoadDialogAsync(string title, FileDialogFilter[] filters);
}