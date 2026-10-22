using System.Collections.ObjectModel;

namespace PerpusKu.ViewModels.Base;

/// <summary>Basis untuk halaman daftar dengan pagination sisi klien.</summary>
public abstract class PagedListViewModel<T> : ViewModelBase
{
    private List<T> _all = new();
    private int _pageIndex;
    private int _pageSize = 10;

    public ObservableCollection<T> PageItems { get; } = new();

    public int PageSize
    {
        get => _pageSize;
        set { if (Set(ref _pageSize, Math.Max(1, value))) { _pageIndex = 0; Refresh(); } }
    }

    public int TotalItems => _all.Count;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(_all.Count / (double)_pageSize));
    public int PageNumber => _pageIndex + 1;
    public bool HasItems => _all.Count > 0;
    public bool IsEmpty => _all.Count == 0;
    public bool CanPrev => _pageIndex > 0;
    public bool CanNext => _pageIndex < TotalPages - 1;

    public string RangeText
    {
        get
        {
            if (_all.Count == 0) return "Tidak ada data";
            int start = _pageIndex * _pageSize + 1;
            int end = Math.Min(_all.Count, start + _pageSize - 1);
            return $"{start}–{end} dari {_all.Count}";
        }
    }

    public RelayCommand PrevCommand => new(() => { if (CanPrev) { _pageIndex--; Refresh(); } });
    public RelayCommand NextCommand => new(() => { if (CanNext) { _pageIndex++; Refresh(); } });

    protected void SetItems(IEnumerable<T> items)
    {
        _all = items.ToList();
        _pageIndex = 0;
        Refresh();
    }

    protected void Refresh()
    {
        if (_pageIndex > TotalPages - 1) _pageIndex = TotalPages - 1;
        PageItems.Clear();
        foreach (var item in _all.Skip(_pageIndex * _pageSize).Take(_pageSize)) PageItems.Add(item);
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageNumber));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanPrev));
        OnPropertyChanged(nameof(CanNext));
        OnPropertyChanged(nameof(RangeText));
    }
}
