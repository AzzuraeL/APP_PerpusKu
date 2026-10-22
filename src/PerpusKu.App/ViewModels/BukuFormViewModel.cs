using PerpusKu.Data.Repositories;
using PerpusKu.Helpers;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class BukuFormViewModel : DialogViewModelBase
{
    private readonly BukuRepository _repo = new();
    private readonly Buku _original;

    private string _kode = "", _judul = "", _penulis = "", _penerbit = "", _tahun = "", _stok = "1", _rak = "";
    private Kategori _kategori;
    private string _eKode, _eJudul, _ePenulis, _ePenerbit, _eTahun, _eStok, _eRak, _eKategori;

    public List<Kategori> KategoriList { get; }
    public bool IsEdit => _original != null;
    public string SubmitText => IsEdit ? "Simpan perubahan" : "Tambah buku";

    public string Kode { get => _kode; set { Set(ref _kode, value); EKode = null; } }
    public string Judul { get => _judul; set { Set(ref _judul, value); EJudul = null; } }
    public string Penulis { get => _penulis; set { Set(ref _penulis, value); EPenulis = null; } }
    public string Penerbit { get => _penerbit; set { Set(ref _penerbit, value); EPenerbit = null; } }
    public string Tahun { get => _tahun; set { Set(ref _tahun, value); ETahun = null; } }
    public string Stok { get => _stok; set { Set(ref _stok, value); EStok = null; } }
    public string Rak { get => _rak; set { Set(ref _rak, value); ERak = null; } }
    public Kategori Kategori { get => _kategori; set { Set(ref _kategori, value); EKategori = null; } }

    public string EKode { get => _eKode; set { Set(ref _eKode, value); OnPropertyChanged(nameof(HasEKode)); } }
    public string EJudul { get => _eJudul; set { Set(ref _eJudul, value); OnPropertyChanged(nameof(HasEJudul)); } }
    public string EPenulis { get => _ePenulis; set { Set(ref _ePenulis, value); OnPropertyChanged(nameof(HasEPenulis)); } }
    public string EPenerbit { get => _ePenerbit; set { Set(ref _ePenerbit, value); OnPropertyChanged(nameof(HasEPenerbit)); } }
    public string ETahun { get => _eTahun; set { Set(ref _eTahun, value); OnPropertyChanged(nameof(HasETahun)); } }
    public string EStok { get => _eStok; set { Set(ref _eStok, value); OnPropertyChanged(nameof(HasEStok)); } }
    public string ERak { get => _eRak; set { Set(ref _eRak, value); OnPropertyChanged(nameof(HasERak)); } }
    public string EKategori { get => _eKategori; set { Set(ref _eKategori, value); OnPropertyChanged(nameof(HasEKategori)); } }
    public bool HasEKode => !string.IsNullOrEmpty(_eKode);
    public bool HasEJudul => !string.IsNullOrEmpty(_eJudul);
    public bool HasEPenulis => !string.IsNullOrEmpty(_ePenulis);
    public bool HasEPenerbit => !string.IsNullOrEmpty(_ePenerbit);
    public bool HasETahun => !string.IsNullOrEmpty(_eTahun);
    public bool HasEStok => !string.IsNullOrEmpty(_eStok);
    public bool HasERak => !string.IsNullOrEmpty(_eRak);
    public bool HasEKategori => !string.IsNullOrEmpty(_eKategori);

    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public BukuFormViewModel(Buku existing, List<Kategori> kategoris)
    {
        _original = existing;
        KategoriList = kategoris;
        Title = existing == null ? "Tambah buku" : "Ubah buku";
        if (existing != null)
        {
            _kode = existing.KodeBuku;
            _judul = existing.Judul;
            _penulis = existing.Penulis;
            _penerbit = existing.Penerbit ?? "";
            _tahun = existing.Tahun?.ToString() ?? "";
            _stok = existing.StokTotal.ToString();
            _rak = existing.Rak ?? "";
            _kategori = kategoris.FirstOrDefault(k => k.KategoriId == existing.KategoriId);
        }
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => Close(false));
    }

    private bool Validate()
    {
        EKode = Validator.Required(Kode, "Kode buku", 20);
        if (EKode == null && !System.Text.RegularExpressions.Regex.IsMatch(Kode.Trim(), @"^[A-Za-z0-9\-]+$"))
            EKode = "Kode hanya boleh huruf, angka, dan tanda minus.";
        EJudul = Validator.Required(Judul, "Judul", 200);
        EPenulis = Validator.Required(Penulis, "Penulis", 120);
        EPenerbit = Validator.OptionalMax(Penerbit, "Penerbit", 120);
        ETahun = Validator.IntRange(Tahun, "Tahun terbit", 1500, DateTime.Now.Year + 1, false);
        EStok = Validator.IntRange(Stok, "Jumlah eksemplar", 0, 999, true);
        ERak = Validator.OptionalMax(Rak, "Rak", 20);
        EKategori = Kategori == null ? "Pilih kategori." : null;
        return !(HasEKode || HasEJudul || HasEPenulis || HasEPenerbit || HasETahun || HasEStok || HasERak || HasEKategori);
    }

    private async Task SaveAsync()
    {
        if (!Validate()) return;
        if (await _repo.KodeExistsAsync(Kode, _original?.BukuId ?? 0))
        {
            EKode = "Kode buku sudah dipakai buku lain.";
            return;
        }

        var buku = new Buku
        {
            BukuId = _original?.BukuId ?? 0,
            KodeBuku = Kode.Trim(),
            Judul = Judul.Trim(),
            Penulis = Penulis.Trim(),
            Penerbit = Penerbit,
            Tahun = string.IsNullOrWhiteSpace(Tahun) ? null : int.Parse(Tahun.Trim()),
            KategoriId = Kategori.KategoriId,
            StokTotal = int.Parse(Stok.Trim()),
            Rak = Rak
        };

        if (IsEdit)
        {
            await _repo.UpdateAsync(buku);
            await LogRepository.AddAsync("Ubah", "Buku", $"Buku \"{buku.Judul}\" diperbarui");
        }
        else
        {
            await _repo.InsertAsync(buku);
            await LogRepository.AddAsync("Tambah", "Buku", $"Buku \"{buku.Judul}\" ditambahkan");
        }
        Close(true);
    }
}
