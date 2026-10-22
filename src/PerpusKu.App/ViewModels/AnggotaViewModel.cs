using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class AnggotaViewModel : PagedListViewModel<Anggota>
{
    private readonly AnggotaRepository _repo = new();
    private CancellationTokenSource _debounce;
    private string _search = "";
    private string _tipe = "Semua";

    public string[] TipeFilter { get; } = { "Semua", "Mahasiswa", "Dosen", "Staf" };
    public bool CanWrite => SessionService.Can(Permission.AnggotaWrite);

    public string SearchText { get => _search; set { if (Set(ref _search, value)) QueueReload(); } }
    public string SelectedTipe { get => _tipe; set { if (Set(ref _tipe, value)) _ = ReloadAsync(); } }

    public AsyncRelayCommand TambahCommand { get; }
    public AsyncRelayCommand EditCommand { get; }
    public AsyncRelayCommand HapusCommand { get; }

    public AnggotaViewModel()
    {
        TambahCommand = new AsyncRelayCommand(_ => TambahAsync(), _ => CanWrite);
        EditCommand = new AsyncRelayCommand(p => EditAsync(p as Anggota), _ => CanWrite);
        HapusCommand = new AsyncRelayCommand(p => HapusAsync(p as Anggota), _ => CanWrite);
        _ = ReloadAsync();
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
        try { SetItems(await _repo.GetAllAsync(_search, _tipe)); }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
        finally { IsBusy = false; }
    }

    private async Task TambahAsync()
    {
        var nomor = await _repo.NextNomorAsync();
        if (DialogService.ShowDialog(new AnggotaFormViewModel(null, nomor)) == true)
        {
            ToastService.Show("Anggota baru berhasil didaftarkan.");
            await ReloadAsync();
        }
    }

    private async Task EditAsync(Anggota a)
    {
        if (a == null) return;
        if (DialogService.ShowDialog(new AnggotaFormViewModel(a, null)) == true)
        {
            ToastService.Show("Data anggota berhasil diperbarui.");
            await ReloadAsync();
        }
    }

    private async Task HapusAsync(Anggota a)
    {
        if (a == null) return;
        if (!DialogService.Confirm("Hapus anggota?", $"{a.Nama} ({a.NomorAnggota}) akan dinonaktifkan. Riwayat peminjaman tetap tersimpan."))
            return;
        await _repo.DeleteAsync(a.AnggotaId);
        await LogRepository.AddAsync("Hapus", "Anggota", $"Anggota {a.Nama} dinonaktifkan");
        ToastService.Show("Anggota dinonaktifkan.");
        await ReloadAsync();
    }
}
