using PerpusKu.Data.Repositories;
using PerpusKu.Helpers;
using PerpusKu.Models;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class AnggotaFormViewModel : DialogViewModelBase
{
    private readonly AnggotaRepository _repo = new();
    private readonly Anggota _original;

    private string _nomor = "", _nama = "", _tipe = "Mahasiswa", _email = "", _telepon = "";
    private string _eNomor, _eNama, _eEmail, _eTelepon;

    public string[] TipeList { get; } = { "Mahasiswa", "Dosen", "Staf" };
    public bool IsEdit => _original != null;
    public string SubmitText => IsEdit ? "Simpan perubahan" : "Daftarkan anggota";

    public string Nomor { get => _nomor; set { Set(ref _nomor, value); ENomor = null; } }
    public string Nama { get => _nama; set { Set(ref _nama, value); ENama = null; } }
    public string Tipe { get => _tipe; set => Set(ref _tipe, value); }
    public string Email { get => _email; set { Set(ref _email, value); EEmail = null; } }
    public string Telepon { get => _telepon; set { Set(ref _telepon, value); ETelepon = null; } }

    public string ENomor { get => _eNomor; set { Set(ref _eNomor, value); OnPropertyChanged(nameof(HasENomor)); } }
    public string ENama { get => _eNama; set { Set(ref _eNama, value); OnPropertyChanged(nameof(HasENama)); } }
    public string EEmail { get => _eEmail; set { Set(ref _eEmail, value); OnPropertyChanged(nameof(HasEEmail)); } }
    public string ETelepon { get => _eTelepon; set { Set(ref _eTelepon, value); OnPropertyChanged(nameof(HasETelepon)); } }
    public bool HasENomor => !string.IsNullOrEmpty(_eNomor);
    public bool HasENama => !string.IsNullOrEmpty(_eNama);
    public bool HasEEmail => !string.IsNullOrEmpty(_eEmail);
    public bool HasETelepon => !string.IsNullOrEmpty(_eTelepon);

    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public AnggotaFormViewModel(Anggota existing, string nomorBaru)
    {
        _original = existing;
        Title = existing == null ? "Daftarkan anggota" : "Ubah data anggota";
        if (existing != null)
        {
            _nomor = existing.NomorAnggota;
            _nama = existing.Nama;
            _tipe = existing.Tipe;
            _email = existing.Email ?? "";
            _telepon = existing.Telepon ?? "";
        }
        else _nomor = nomorBaru ?? "";
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => Close(false));
    }

    private bool Validate()
    {
        ENomor = Validator.Required(Nomor, "Nomor anggota", 20);
        ENama = Validator.Required(Nama, "Nama", 120);
        EEmail = Validator.Email(Email) ?? Validator.OptionalMax(Email, "Email", 120);
        ETelepon = Validator.Phone(Telepon) ?? Validator.OptionalMax(Telepon, "Telepon", 30);
        return !(HasENomor || HasENama || HasEEmail || HasETelepon);
    }

    private async Task SaveAsync()
    {
        if (!Validate()) return;
        if (await _repo.NomorExistsAsync(Nomor, _original?.AnggotaId ?? 0))
        {
            ENomor = "Nomor anggota sudah dipakai.";
            return;
        }
        var a = new Anggota
        {
            AnggotaId = _original?.AnggotaId ?? 0,
            NomorAnggota = Nomor.Trim(),
            Nama = Nama.Trim(),
            Tipe = Tipe,
            Email = Email,
            Telepon = Telepon
        };
        if (IsEdit)
        {
            await _repo.UpdateAsync(a);
            await LogRepository.AddAsync("Ubah", "Anggota", $"Data anggota {a.Nama} diperbarui");
        }
        else
        {
            await _repo.InsertAsync(a);
            await LogRepository.AddAsync("Tambah", "Anggota", $"Anggota {a.Nama} terdaftar");
        }
        Close(true);
    }
}
