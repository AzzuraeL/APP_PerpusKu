using System.Collections.ObjectModel;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class NavItem : ViewModelBase
{
    private bool _isActive;
    public string Key { get; init; }
    public string Title { get; init; }
    public string Subtitle { get; init; }
    public string Glyph { get; init; }
    public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }
}

public class ShellViewModel : ViewModelBase
{
    public static ShellViewModel Instance { get; private set; }

    private ViewModelBase _currentPage;
    private NavItem _selected;
    private bool _railExpanded = true;

    public ObservableCollection<NavItem> Items { get; } = new();
    public ViewModelBase CurrentPage { get => _currentPage; private set => Set(ref _currentPage, value); }
    public NavItem Selected { get => _selected; private set { Set(ref _selected, value); OnPropertyChanged(nameof(PageTitle)); OnPropertyChanged(nameof(PageSubtitle)); } }
    public bool RailExpanded { get => _railExpanded; set => Set(ref _railExpanded, value); }

    public string PageTitle => Selected?.Title;
    public string PageSubtitle => Selected?.Subtitle;
    public User User => SessionService.Current;
    public string Today => DateTime.Now.ToString("dddd, dd MMMM yyyy");
    public ObservableCollection<ToastItem> Toasts => ToastService.Items;
    public bool IsDark => ThemeService.IsDark;
    public string ThemeGlyph => ThemeService.IsDark ? "sun" : "moon";

    public RelayCommand NavigateCommand { get; }
    public RelayCommand ToggleRailCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand LogoutCommand { get; }
    public event Action LogoutRequested;

    public ShellViewModel()
    {
        Instance = this;
        Items.Add(new NavItem { Key = "dashboard", Title = "Beranda", Subtitle = "Ringkasan perpustakaan hari ini", Glyph = "house" });
        Items.Add(new NavItem { Key = "buku", Title = "Katalog Buku", Subtitle = "Cari dan kelola koleksi buku", Glyph = "library" });
        if (SessionService.Can(Permission.AnggotaWrite))
            Items.Add(new NavItem { Key = "anggota", Title = "Anggota", Subtitle = "Data mahasiswa, dosen, dan staf", Glyph = "users" });
        if (SessionService.Can(Permission.Sirkulasi))
        {
            Items.Add(new NavItem { Key = "sirkulasi", Title = "Meja Sirkulasi", Subtitle = "Pinjam, kembalikan, dan perpanjang buku", Glyph = "refresh-ccw" });
            Items.Add(new NavItem { Key = "riwayat", Title = "Riwayat", Subtitle = "Seluruh transaksi peminjaman", Glyph = "history" });
        }
        if (SessionService.Can(Permission.Laporan))
            Items.Add(new NavItem { Key = "laporan", Title = "Laporan", Subtitle = "Ringkasan periode dan ekspor PDF", Glyph = "file-text" });
        Items.Add(new NavItem { Key = "pengaturan", Title = "Pengaturan", Subtitle = "Tema, akun, dan aturan perpustakaan", Glyph = "settings" });

        NavigateCommand = new RelayCommand(p => { if (p is NavItem n) Navigate(n.Key); else if (p is string s) Navigate(s); });
        ToggleRailCommand = new RelayCommand(() => RailExpanded = !RailExpanded);
        ToggleThemeCommand = new RelayCommand(() =>
        {
            ThemeService.Toggle();
            OnPropertyChanged(nameof(IsDark));
            OnPropertyChanged(nameof(ThemeGlyph));
        });
        LogoutCommand = new RelayCommand(() =>
        {
            if (DialogService.Confirm("Keluar dari PerpusKu?", "Sesi Anda akan ditutup dan Anda kembali ke halaman login.", "Keluar", false))
                LogoutRequested?.Invoke();
        });

        Navigate("dashboard");
    }

    public void Navigate(string key)
    {
        var item = Items.FirstOrDefault(i => i.Key == key);
        if (item == null) return;
        foreach (var i in Items) i.IsActive = i == item;
        Selected = item;
        CurrentPage = key switch
        {
            "dashboard" => new DashboardViewModel(),
            "buku" => new BukuViewModel(),
            "anggota" => new AnggotaViewModel(),
            "sirkulasi" => new SirkulasiViewModel(),
            "riwayat" => new RiwayatViewModel(),
            "laporan" => new LaporanViewModel(),
            "pengaturan" => new PengaturanViewModel(this),
            _ => null
        };
    }

    public void NotifyThemeChanged()
    {
        OnPropertyChanged(nameof(IsDark));
        OnPropertyChanged(nameof(ThemeGlyph));
    }
}
