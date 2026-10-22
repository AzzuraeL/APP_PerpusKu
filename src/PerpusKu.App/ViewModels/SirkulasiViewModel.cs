using System.Collections.ObjectModel;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class SirkulasiViewModel : ViewModelBase
{
    private readonly SirkulasiService _service = new();
    private CancellationTokenSource _debounceKembali;
    private int _lookupVersion;

    private string _nomorAnggota = "";
    private string _kodeBuku = "";
    private string _keyword = "";
    private Anggota _anggota;
    private Buku _buku;
    private bool _anggotaTidakAda;
    private bool _bukuTidakAda;

    public string NomorAnggota { get => _nomorAnggota; set { if (Set(ref _nomorAnggota, value)) _ = LookupAnggotaAsync(); } }
    public string KodeBuku { get => _kodeBuku; set { if (Set(ref _kodeBuku, value)) _ = LookupBukuAsync(); } }
    public string Keyword { get => _keyword; set { if (Set(ref _keyword, value)) QueueReloadAktif(); } }

    public Anggota AnggotaInfo { get => _anggota; private set { Set(ref _anggota, value); OnPropertyChanged(nameof(AdaAnggota)); } }
    public Buku BukuInfo { get => _buku; private set { Set(ref _buku, value); OnPropertyChanged(nameof(AdaBuku)); } }
    public bool AdaAnggota => _anggota != null;
    public bool AdaBuku => _buku != null;
    public bool AnggotaTidakAda { get => _anggotaTidakAda; private set => Set(ref _anggotaTidakAda, value); }
    public bool BukuTidakAda { get => _bukuTidakAda; private set => Set(ref _bukuTidakAda, value); }

    public ObservableCollection<Peminjaman> Aktif { get; } = new();
    public bool AktifKosong => Aktif.Count == 0;
    public string DendaInfo => $"Denda keterlambatan {SirkulasiService.Rupiah(SettingsCache.DendaPerHari)} per hari · lama pinjam {SettingsCache.LamaPinjamHari} hari";

    public AsyncRelayCommand PinjamCommand { get; }
    public AsyncRelayCommand KembalikanCommand { get; }
    public AsyncRelayCommand PerpanjangCommand { get; }
    public RelayCommand BersihkanCommand { get; }

    public SirkulasiViewModel()
    {
        PinjamCommand = new AsyncRelayCommand(PinjamAsync,
            () => !string.IsNullOrWhiteSpace(_nomorAnggota) && !string.IsNullOrWhiteSpace(_kodeBuku));
        KembalikanCommand = new AsyncRelayCommand(p => KembalikanAsync(p as Peminjaman));
        PerpanjangCommand = new AsyncRelayCommand(p => PerpanjangAsync(p as Peminjaman));
        BersihkanCommand = new RelayCommand(() => { NomorAnggota = ""; KodeBuku = ""; });
        _ = ReloadAktifAsync();
    }

    private async Task LookupAnggotaAsync()
    {
        int v = ++_lookupVersion;
        var key = _nomorAnggota?.Trim();
        if (string.IsNullOrEmpty(key)) { AnggotaInfo = null; AnggotaTidakAda = false; return; }
        try
        {
            var a = await _service.CariAnggotaAsync(key);
            if (v != _lookupVersion && key != _nomorAnggota?.Trim()) return;
            AnggotaInfo = a;
            AnggotaTidakAda = a == null && key.Length >= 4;
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
    }

    private async Task LookupBukuAsync()
    {
        var key = _kodeBuku?.Trim();
        if (string.IsNullOrEmpty(key)) { BukuInfo = null; BukuTidakAda = false; return; }
        try
        {
            var b = await _service.CariBukuAsync(key);
            if (key != _kodeBuku?.Trim()) return; // input sudah berubah lagi
            BukuInfo = b;
            BukuTidakAda = b == null && key.Length >= 4;
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
    }

    private async Task PinjamAsync()
    {
        var msg = await _service.PinjamAsync(_kodeBuku, _nomorAnggota);
        ToastService.Show(msg);
        KodeBuku = "";
        await ReloadAktifAsync();
        await LookupAnggotaAsync(); // perbarui jumlah pinjaman anggota
    }

    private async Task KembalikanAsync(Peminjaman p)
    {
        if (p == null) return;
        var denda = p.DendaTampil;
        var pesan = denda > 0
            ? $"\"{p.Judul}\" terlambat {p.HariTerlambat} hari. Denda {SirkulasiService.Rupiah(denda)} akan dicatat."
            : $"Kembalikan \"{p.Judul}\" dari {p.NamaAnggota}?";
        if (!DialogService.Confirm("Konfirmasi pengembalian", pesan, "Kembalikan", false)) return;

        var msg = await _service.KembalikanAsync(p.PeminjamanId);
        ToastService.Show(msg);
        await ReloadAktifAsync();
        await LookupAnggotaAsync();
        await LookupBukuAsync();
    }

    private async Task PerpanjangAsync(Peminjaman p)
    {
        if (p == null) return;
        var msg = await _service.PerpanjangAsync(p.PeminjamanId);
        ToastService.Show(msg);
        await ReloadAktifAsync();
    }

    private void QueueReloadAktif()
    {
        _debounceKembali?.Cancel();
        _debounceKembali = new CancellationTokenSource();
        var token = _debounceKembali.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ReloadAktifAsync());
            }
            catch (TaskCanceledException) { }
        });
    }

    public async Task ReloadAktifAsync()
    {
        try
        {
            var list = await _service.CariAktifAsync(_keyword);
            Aktif.Clear();
            foreach (var p in list) Aktif.Add(p);
            OnPropertyChanged(nameof(AktifKosong));
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
    }
}
