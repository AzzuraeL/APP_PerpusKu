using System.Windows;
using PerpusKu.ViewModels;
using PerpusKu.ViewModels.Base;
using PerpusKu.Views.Dialogs;

namespace PerpusKu.Services;

public static class DialogService
{
    public static bool? ShowDialog(DialogViewModelBase vm)
    {
        var window = new DialogWindow { DataContext = vm };
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != window)
                    ?? Application.Current.MainWindow;
        if (owner != null && owner != window && owner.IsVisible) window.Owner = owner;
        vm.CloseRequested += result =>
        {
            try { window.DialogResult = result; }
            catch (InvalidOperationException) { window.Close(); }
        };
        return window.ShowDialog();
    }

    public static bool Confirm(string title, string message, string confirmText = "Ya, hapus", bool danger = true)
        => ShowDialog(new ConfirmViewModel(title, message, confirmText, danger)) == true;
}
