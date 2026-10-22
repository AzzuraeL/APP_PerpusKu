using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace PerpusKu.Services;

public enum ToastKind { Success, Error, Info }

public class ToastItem
{
    public string Message { get; init; }
    public ToastKind Kind { get; init; }
    public string Glyph => Kind switch { ToastKind.Success => "check", ToastKind.Error => "circle-x", _ => "info" };
    public string AccentKey => Kind switch { ToastKind.Success => "SuccessBrush", ToastKind.Error => "DangerBrush", _ => "PrimaryBrush" };
}

public static class ToastService
{
    public static ObservableCollection<ToastItem> Items { get; } = new();

    public static void Show(string message, ToastKind kind = ToastKind.Success)
    {
        var item = new ToastItem { Message = message, Kind = kind };
        Items.Add(item);
        while (Items.Count > 4) Items.RemoveAt(0);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(kind == ToastKind.Error ? 6 : 3.5) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Items.Remove(item);
        };
        timer.Start();
    }
}
