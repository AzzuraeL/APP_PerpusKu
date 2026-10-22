using PerpusKu.Data;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _auth = new();
    private string _username = "";
    private string _error;
    private string _status;
    private bool _dbReady;
    private bool _dbFailed;

    public string Username { get => _username; set => Set(ref _username, value); }
    public string Password { get; set; } = "";
    public string Error { get => _error; set { Set(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(_error);
    public string Status { get => _status; set => Set(ref _status, value); }
    public bool DbReady { get => _dbReady; set { Set(ref _dbReady, value); OnPropertyChanged(nameof(CanLogin)); } }
    public bool DbFailed { get => _dbFailed; set => Set(ref _dbFailed, value); }
    public bool CanLogin => DbReady && !IsBusy;

    public event Action LoginSucceeded;

    public AsyncRelayCommand LoginCommand { get; }
    public AsyncRelayCommand RetryCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => CanLogin);
        RetryCommand = new AsyncRelayCommand(InitializeAsync);
    }

    public async Task InitializeAsync()
    {
        DbFailed = false;
        DbReady = false;
        Error = null;
        Status = "Menyiapkan database…";
        IsBusy = true;
        try
        {
            await DatabaseInitializer.EnsureAsync();
            DbReady = true;
            Status = null;
        }
        catch (Exception ex)
        {
            ErrorHandler.Log(ex);
            DbFailed = true;
            Status = "Database belum bisa diakses. Pastikan SQL Server LocalDB terpasang (komponen bawaan Visual Studio) " +
                     "atau ubah connection string di appsettings.json.";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanLogin));
        }
    }

    private async Task LoginAsync()
    {
        Error = null;
        IsBusy = true;
        OnPropertyChanged(nameof(CanLogin));
        try
        {
            var (ok, err) = await _auth.LoginAsync(Username, Password);
            if (!ok) { Error = err; return; }
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorHandler.Log(ex);
            Error = ErrorHandler.ToFriendlyMessage(ex);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanLogin));
        }
    }
}
