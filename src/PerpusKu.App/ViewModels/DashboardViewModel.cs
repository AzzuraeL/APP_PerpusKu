using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;
using SkiaSharp;

namespace PerpusKu.ViewModels;

public class LegendItem
{
    public string Nama { get; init; }
    public int Jumlah { get; init; }
    public string Warna { get; init; }
    public System.Windows.Media.Brush Brush => new System.Windows.Media.SolidColorBrush(
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Warna));
}

public class DashboardViewModel : ViewModelBase
{
    private static readonly string[] Palette =
        { "#FF7F50", "#1A237E", "#B71C1C", "#FF9470", "#8F96E8", "#2D2D2D", "#C5C9EC", "#EA754A" };
    private static readonly string[] PaletteDark =
        { "#FF2D2D", "#E8C547", "#D4D4D4", "#F3793A", "#8F8F8F", "#4A4A4A", "#EA2929", "#B89B2E" };

    private readonly DashboardRepository _repo = new();
    private List<TitikData> _tren = new();
    private List<TitikData> _kategori = new();

    private DashboardStat _stat = new();
    public DashboardStat Stat { get => _stat; private set => Set(ref _stat, value); }

    public ObservableCollection<Peminjaman> PerluPerhatian { get; } = new();
    public ObservableCollection<LogAktivitas> Aktivitas { get; } = new();
    public ObservableCollection<LegendItem> Legend { get; } = new();

    private ISeries[] _trenSeries = Array.Empty<ISeries>();
    private Axis[] _trenX = Array.Empty<Axis>();
    private Axis[] _trenY = Array.Empty<Axis>();
    private ISeries[] _kategoriSeries = Array.Empty<ISeries>();

    public ISeries[] TrenSeries { get => _trenSeries; private set => Set(ref _trenSeries, value); }
    public Axis[] TrenXAxes { get => _trenX; private set => Set(ref _trenX, value); }
    public Axis[] TrenYAxes { get => _trenY; private set => Set(ref _trenY, value); }
    public ISeries[] KategoriSeries { get => _kategoriSeries; private set => Set(ref _kategoriSeries, value); }

    public bool AdaPerhatian => PerluPerhatian.Count > 0;
    public bool TidakAdaPerhatian => PerluPerhatian.Count == 0;
    public string Sapaan
    {
        get
        {
            var h = DateTime.Now.Hour;
            var waktu = h < 11 ? "pagi" : h < 15 ? "siang" : h < 18 ? "sore" : "malam";
            var nama = SessionService.Current?.NamaLengkap?.Split(' ').FirstOrDefault() ?? "";
            return $"Selamat {waktu}, {nama}";
        }
    }

    public RelayCommand LihatRiwayatCommand { get; } = new(() => ShellViewModel.Instance?.Navigate(SessionService.Can(Permission.Sirkulasi) ? "riwayat" : "buku"));
    public RelayCommand KeSirkulasiCommand { get; } = new(() => ShellViewModel.Instance?.Navigate("sirkulasi"), () => SessionService.Can(Permission.Sirkulasi));

    public DashboardViewModel()
    {
        ThemeService.Changed += OnThemeChanged;
        _ = LoadAsync();
    }

    private void OnThemeChanged()
    {
        if (_tren.Count > 0) BuildCharts();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Stat = await _repo.GetStatsAsync();
            _tren = await _repo.GetTrenPinjamAsync(7);
            _kategori = await _repo.GetBukuPerKategoriAsync();
            var perhatian = await _repo.GetPerluPerhatianAsync(6);
            var aktivitas = await _repo.GetAktivitasAsync(6);

            PerluPerhatian.Clear();
            foreach (var p in perhatian) PerluPerhatian.Add(p);
            Aktivitas.Clear();
            foreach (var a in aktivitas) Aktivitas.Add(a);
            OnPropertyChanged(nameof(AdaPerhatian));
            OnPropertyChanged(nameof(TidakAdaPerhatian));
            BuildCharts();
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildCharts()
    {
        var text = SKColor.Parse(ThemeService.IsDark ? "#8F8F8F" : "#6E6A5F");
        var grid = SKColor.Parse(ThemeService.IsDark ? "#4A4A4A" : "#DCD5C4");
        var teal = SKColor.Parse(ThemeService.IsDark ? "#FF2D2D" : "#FF7F50");

        TrenSeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = _tren.Select(t => t.Nilai).ToArray(),
                Name = "Peminjaman",
                Stroke = new SolidColorPaint(teal, 3),
                Fill = new SolidColorPaint(teal.WithAlpha(40)),
                GeometrySize = 9,
                GeometryStroke = new SolidColorPaint(teal, 3),
                GeometryFill = new SolidColorPaint(SKColor.Parse(ThemeService.IsDark ? "#5C0A0A" : "#FFFFFF")),
                LineSmoothness = 0.5
            }
        };
        TrenXAxes = new[]
        {
            new Axis { Labels = _tren.Select(t => t.Label).ToArray(), LabelsPaint = new SolidColorPaint(text), TextSize = 12, SeparatorsPaint = null }
        };
        TrenYAxes = new[]
        {
            new Axis { MinLimit = 0, MinStep = 1, LabelsPaint = new SolidColorPaint(text), TextSize = 12, SeparatorsPaint = new SolidColorPaint(grid, 1) }
        };

        var series = new List<ISeries>();
        Legend.Clear();
        var palette = ThemeService.IsDark ? PaletteDark : Palette;
        int i = 0;
        foreach (var k in _kategori)
        {
            var warna = palette[i % palette.Length];
            series.Add(new PieSeries<int>
            {
                Values = new[] { k.Nilai },
                Name = k.Label,
                InnerRadius = 58,
                Fill = new SolidColorPaint(SKColor.Parse(warna)),
                Stroke = new SolidColorPaint(SKColor.Parse(ThemeService.IsDark ? "#5C0A0A" : "#FFFFFF"), 2)
            });
            Legend.Add(new LegendItem { Nama = k.Label, Jumlah = k.Nilai, Warna = warna });
            i++;
        }
        KategoriSeries = series.ToArray();
    }
}
