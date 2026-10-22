using System.Windows;
using System.Windows.Input;
using PerpusKu.ViewModels;

namespace PerpusKu.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm = new();

    public LoginWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoginSucceeded += () =>
        {
            App.ShowShell();   // tampilkan shell dulu agar aplikasi tidak tertutup
            Close();
        };
        Loaded += async (_, _) =>
        {
            UsernameBox.Focus();
            await _vm.InitializeAsync();
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => _vm.Password = PasswordBox.Password;
    private void Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
