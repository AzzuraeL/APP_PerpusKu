using System.Windows;
using System.Windows.Controls;
using PerpusKu.ViewModels;

namespace PerpusKu.Views.Pages;

public partial class PengaturanView : UserControl
{
    public PengaturanView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is PengaturanViewModel vm)
                vm.PasswordFieldsCleared += () =>
                {
                    OldPw.Clear();
                    NewPw.Clear();
                    ConfirmPw.Clear();
                };
        };
    }

    // PasswordBox tidak mendukung binding, jadi nilainya diteruskan ke ViewModel dari sini.
    private void OldPw_Changed(object sender, RoutedEventArgs e) { if (DataContext is PengaturanViewModel vm) vm.OldPassword = OldPw.Password; }
    private void NewPw_Changed(object sender, RoutedEventArgs e) { if (DataContext is PengaturanViewModel vm) vm.NewPassword = NewPw.Password; }
    private void ConfirmPw_Changed(object sender, RoutedEventArgs e) { if (DataContext is PengaturanViewModel vm) vm.ConfirmPassword = ConfirmPw.Password; }
}
