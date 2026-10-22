using System.Windows;
using PerpusKu.Services;
using PerpusKu.ViewModels;

namespace PerpusKu.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is ShellViewModel vm)
                vm.LogoutRequested += () =>
                {
                    SessionService.SignOut();
                    App.ShowLogin();   // tampilkan login dulu, baru tutup shell
                    Close();
                };
        };
    }
}
