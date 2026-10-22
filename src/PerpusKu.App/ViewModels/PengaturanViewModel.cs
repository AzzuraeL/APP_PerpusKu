using System.Collections.ObjectModel;
using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class EditableSetting : ViewModelBase
{
    private string _nilai;
    public string Kunci { get; init; }
    public string Label { get; init; }
    public string Nilai { get => _nilai; set => Set(ref _nilai, value); }
}

public class PengaturanViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;
    private readonly AuthService _auth = new();
    private readonly PengaturanRepository _repo = new();

    private string _oldPw = "", _newPw = "", _confirmPw = "";
    private string _pwError;
    private string _rulesError;

    public User User => SessionService.Current;
    public bool IsDark => ThemeService.IsDark;
    public bool IsLight => !ThemeService.IsDark;
    public bool CanEditRules => SessionService.Can(Permission.KelolaPengaturan);

    public ObservableCollection<EditableSetting> Rules { get; } = new();

    public string OldPassword { get => _oldPw; set => Set(ref _oldPw, value); }
    public string NewPassword { get => _newPw; set => Set(ref _newPw, value); }
    public string ConfirmPassword { get => _confirmPw; set => Set(ref _confirmPw, value); }
    public string PasswordError { get => _pwError; private set { Set(ref _pwError, value); OnPropertyChanged(nameof(HasPasswordError)); } }
    public bool HasPasswordError => !string.IsNullOrEmpty(_pwError);
    public string RulesError { get => _rulesError; private set { Set(ref _rulesError, value); OnPropertyChanged(nameof(HasRulesError)); } }
    public bool HasRulesError => !string.IsNullOrEmpty(_rulesError);

    public RelayCommand SetLightCommand { get; }
    public RelayCommand SetDarkCommand { get; }
    public AsyncRelayCommand ChangePasswordCommand { get; }
    public AsyncRelayCommand SaveRulesCommand { get; }

    public event Action PasswordFieldsCleared;

    public PengaturanViewModel(ShellViewModel shell)
    {
        _shell = shell;
        SetLightCommand = new RelayCommand(() => ApplyTheme(false));
        SetDarkCommand = new RelayCommand(() => ApplyTheme(true));
        ChangePasswordCommand = new AsyncRelayCommand(ChangePasswordAsync);
        SaveRulesCommand = new AsyncRelayCommand(SaveRulesAsync, () => CanEditRules);
        _ = LoadRulesAsync();
    }

    private void ApplyTheme(bool dark)
    {
        ThemeService.Apply(dark);
        OnPropertyChanged(nameof(IsDark));
        OnPropertyChanged(nameof(IsLight));
        _shell.NotifyThemeChanged();
    }

    private async Task LoadRulesAsync()
    {
        try
        {
            var items = await _repo.GetAllAsync();
            Rules.Clear();
            foreach (var i in items) Rules.Add(new EditableSetting { Kunci = i.Kunci, Label = i.Label, Nilai = i.Nilai });
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
    }

    private async Task ChangePasswordAsync()
    {
        PasswordError = null;
        if (string.IsNullOrEmpty(_oldPw)) { PasswordError = "Password lama wajib diisi."; return; }
        if (_newPw != _confirmPw) { PasswordError = "Konfirmasi password tidak sama."; return; }
        var err = await _auth.ChangePasswordAsync(_oldPw, _newPw);
        if (err != null) { PasswordError = err; return; }
        ToastService.Show("Password berhasil diubah.");
        PasswordFieldsCleared?.Invoke();
    }

    private async Task SaveRulesAsync()
    {
        RulesError = null;
        foreach (var r in Rules)
        {
            if (!int.TryParse(r.Nilai?.Trim(), out var n) || n < 0 || n > 100000)
            {
                RulesError = $"\"{r.Label}\" harus berupa angka bulat 0–100000.";
                return;
            }
            if (r.Kunci != "DendaPerHari" && n < 1)
            {
                RulesError = $"\"{r.Label}\" minimal 1.";
                return;
            }
        }
        foreach (var r in Rules) await _repo.UpdateAsync(r.Kunci, r.Nilai.Trim());
        await PengaturanRepository.LoadCacheAsync();
        await LogRepository.AddAsync("Ubah", "Pengaturan", "Aturan perpustakaan diperbarui");
        ToastService.Show("Aturan perpustakaan disimpan.");
    }
}
