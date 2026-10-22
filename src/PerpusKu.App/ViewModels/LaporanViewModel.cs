using System.Collections.ObjectModel;
using Microsoft.Win32;
using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class LaporanViewModel : ViewModelBase
{
    private readonly DashboardRepository _dash = new();
    private readonly PeminjamanRepository _pinjam = new();
    private readonly ReportService _report = new();

    private DateTime? _dari = DateTime.Today.AddDays(-29);
    private DateTime? _sampai = DateTime.Today;
    private RingkasanLaporan _ringkasan = new();
    private string _error;

    public DateTime? Dari { get => _dari; set => Set(ref _dari, value); }
    public DateTime? Sampai { get => _sampai; set => Set(ref _sampai, value); }
    public RingkasanLaporan Ringkasan { get => _ringkasan; private set => Set(ref _ringkasan, value); }
    public string Error { get => _error; private set { Set(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(_error);

    public ObservableCollection<BukuPopuler> Populer { get; } = new();
    public ObservableCollection<Peminjaman> Rows { get; } = new();
    public bool PopulerKosong => Populer.Count == 0;
    public bool RowsKosong => Rows.Count == 0;
    public string RowsInfo => $"{Rows.Count} transaksi";

    public AsyncRelayCommand TerapkanCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }
    public RelayCommand PresetCommand { get; }

    public LaporanViewModel()
    {
        TerapkanCommand = new AsyncRelayCommand(LoadAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        PresetCommand = new RelayCommand(p =>
        {
            var today = DateTime.Today;
            switch (p as string)
            {
                case "7": Dari = today.AddDays(-6); break;
                case "30": Dari = today.AddDays(-29); break;
                case "bulan": Dari = new DateTime(today.Year, today.Month, 1); break;
            }
            Sampai = today;
            _ = LoadAsync();
        });
        _ = LoadAsync();
    }

    private bool ValidateRange()
    {
        if (_dari == null || _sampai == null) { Error = "Tanggal awal dan akhir wajib diisi."; return false; }
        if (_dari > _sampai) { Error = "Tanggal awal tidak boleh setelah tanggal akhir."; return false; }
        Error = null;
        return true;
    }

    private async Task LoadAsync()
    {
        if (!ValidateRange()) return;
        IsBusy = true;
        try
        {
            Ringkasan = await _dash.GetRingkasanAsync(_dari.Value, _sampai.Value);
            var populer = await _dash.GetBukuPopulerAsync(_dari.Value, _sampai.Value, 5);
            var rows = await _pinjam.QueryAsync("", "Semua", _dari, _sampai);
            Populer.Clear(); foreach (var b in populer) Populer.Add(b);
            Rows.Clear(); foreach (var r in rows) Rows.Add(r);
            OnPropertyChanged(nameof(PopulerKosong));
            OnPropertyChanged(nameof(RowsKosong));
            OnPropertyChanged(nameof(RowsInfo));
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
        finally { IsBusy = false; }
    }

    private async Task ExportAsync()
    {
        if (!ValidateRange()) return;
        var dlg = new SaveFileDialog
        {
            Filter = "Dokumen PDF (*.pdf)|*.pdf",
            FileName = $"Laporan_PerpusKu_{_dari:yyyyMMdd}-{_sampai:yyyyMMdd}.pdf",
            Title = "Simpan laporan"
        };
        if (dlg.ShowDialog() != true) return;

        IsBusy = true;
        try
        {
            await _report.ExportPdfAsync(dlg.FileName, _dari.Value, _sampai.Value);
            await LogRepository.AddAsync("Ekspor", "Laporan", $"Laporan PDF periode {_dari:dd/MM/yyyy}–{_sampai:dd/MM/yyyy} diekspor");
            ToastService.Show("Laporan PDF berhasil disimpan.");
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); }
            catch { /* membuka file bersifat opsional */ }
        }
        finally { IsBusy = false; }
    }
}
