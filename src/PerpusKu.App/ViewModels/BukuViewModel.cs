using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class BukuViewModel : PagedListViewModel<Buku>
{
    private readonly BukuRepository _repo = new();
    private readonly KategoriRepository _kategoriRepo = new();
    private CancellationTokenSource _debounce;

    private string _search = "";
    private Kategori _selectedKategori;
    private bool _hanyaTersedia;
    private bool _isCardView = true;
    private List<Kategori> _kategoriDb = new();

    public List<Kategori> KategoriFilter { get; private set; } = new();
    public bool CanWrite => SessionService.Can(Permission.BukuWrite);

    public string SearchText { get => _search; set { if (Set(ref _search, value)) QueueReload(); } }
    public Kategori SelectedKategori { get => _selectedKategori; set { if (Set(ref _selectedKategori, value)) _ = ReloadAsync(); } }
    public bool HanyaTersedia { get => _hanyaTersedia; set { if (Set(ref _hanyaTersedia, value)) _ = ReloadAsync(); } }
    public bool IsCardView
    {
        get => _isCardView;
        set { if (Set(ref _isCardView, value)) { PageSize = value ? 12 : 10; OnPropertyChanged(nameof(IsTableView)); } }
    }
    public bool IsTableView => !_isCardView;

    public AsyncRelayCommand TambahCommand { get; }
    public AsyncRelayCommand EditCommand { get; }
    public AsyncRelayCommand HapusCommand { get; }
    public RelayCommand SetCardViewCommand { get; }
    public RelayCommand SetTableViewCommand { get; }

    public BukuViewModel()
    {
        PageSize = 12;
        TambahCommand = new AsyncRelayCommand(_ => TambahAsync(), _ => CanWrite);
        EditCommand = new AsyncRelayCommand(p => EditAsync(p as Buku), _ => CanWrite);
        HapusCommand = new AsyncRelayCommand(p => HapusAsync(p as Buku), _ => CanWrite);
        SetCardViewCommand = new RelayCommand(() => IsCardView = true);
        SetTableViewCommand = new RelayCommand(() => IsCardView = false);
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        try
        {
            _kategoriDb = await _kategoriRepo.GetAllAsync();
            KategoriFilter = new List<Kategori> { new() { KategoriId = 0, Nama = "Semua kategori" } };
            KategoriFilter.AddRange(_kategoriDb);
            _selectedKategori = KategoriFilter[0];
            OnPropertyChanged(nameof(KategoriFilter));
            OnPropertyChanged(nameof(SelectedKategori));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
    }

    private void QueueReload()
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => ReloadAsync());
            }
            catch (TaskCanceledException) { }
        });
    }

    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            int? kat = _selectedKategori == null || _selectedKategori.KategoriId == 0 ? null : _selectedKategori.KategoriId;
            SetItems(await _repo.GetAllAsync(_search, kat, _hanyaTersedia));
        }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
        finally { IsBusy = false; }
    }

    private async Task TambahAsync()
    {
        var vm = new BukuFormViewModel(null, _kategoriDb);
        if (DialogService.ShowDialog(vm) == true)
        {
            ToastService.Show("Buku baru berhasil ditambahkan.");
            await ReloadAsync();
        }
    }

    private async Task EditAsync(Buku buku)
    {
        if (buku == null) return;
        var vm = new BukuFormViewModel(buku, _kategoriDb);
        if (DialogService.ShowDialog(vm) == true)
        {
            ToastService.Show("Data buku berhasil diperbarui.");
            await ReloadAsync();
        }
    }

    private async Task HapusAsync(Buku buku)
    {
        if (buku == null) return;
        if (!DialogService.Confirm("Hapus buku?", $"\"{buku.Judul}\" akan dihapus dari katalog. Riwayat peminjaman tetap tersimpan."))
            return;
        await _repo.DeleteAsync(buku.BukuId);
        await LogRepository.AddAsync("Hapus", "Buku", $"Buku \"{buku.Judul}\" dihapus");
        ToastService.Show("Buku dihapus dari katalog.");
        await ReloadAsync();
    }
}
