using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using PerpusKu.Services;
using PerpusKu.ViewModels;
using PerpusKu.Views;

namespace PerpusKu;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var culture = new CultureInfo("id-ID");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        DispatcherUnhandledException += (_, args) =>
        {
            ErrorHandler.Handle(args.Exception);
            args.Handled = true;
        };

        ThemeService.LoadSaved();
        ShowLogin();
    }

    public static void ShowLogin()
    {
        var login = new LoginWindow();
        Current.MainWindow = login;
        login.Show();
    }

    public static void ShowShell()
    {
        var shell = new ShellWindow { DataContext = new ShellViewModel() };
        Current.MainWindow = shell;
        shell.Show();
    }
}
