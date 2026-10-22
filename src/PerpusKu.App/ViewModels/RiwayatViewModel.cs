using PerpusKu.Data.Repositories;
using PerpusKu.Models;
using PerpusKu.Services;
using PerpusKu.ViewModels.Base;

namespace PerpusKu.ViewModels;

public class RiwayatViewModel : PagedListViewModel<Peminjaman>
{
    private readonly PeminjamanRepository _repo = new();
    private CancellationTokenSource _debounce;
    private string _search = "";
    private string _status = "Semua";

    public string[] StatusFilter { get; } = { "Semua", "Dipinjam", "Terlambat", "Dikembalikan" };
    public string SearchText { get => _search; set { if (Set(ref _search, value)) QueueReload(); } }
    public string SelectedStatus { get => _status; set { if (Set(ref _status, value)) _ = ReloadAsync(); } }

    public RiwayatViewModel() { _ = ReloadAsync(); }

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
        try { SetItems(await _repo.QueryAsync(_search, _status)); }
        catch (Exception ex) { ErrorHandler.Handle(ex); }
        finally { IsBusy = false; }
    }
}
