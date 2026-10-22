using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PerpusKu.Converters;

internal static class Res
{
    public static Brush Get(string key) => Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
}

/// <summary>Kunci icon Lucide ("check") → Geometry resource "Icon.check".</summary>
public class IconGeomConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Application.Current?.TryFindResource($"Icon.{value as string ?? "info"}") as Geometry;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : true;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : true;
}

/// <summary>Teks status → warna latar lembut untuk pill.</summary>
public class StatusSoftBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Res.Get((value as string) switch
        {
            "Tersedia" or "Dikembalikan" => "SuccessSoftBrush",
            "Terlambat" or "Habis" => "DangerSoftBrush",
            "Jatuh tempo hari ini" => "WarnSoftBrush",
            _ => "PrimarySoftBrush"
        });
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Teks status → warna teks untuk pill.</summary>
public class StatusFgBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Res.Get((value as string) switch
        {
            "Tersedia" or "Dikembalikan" => "SuccessBrush",
            "Terlambat" or "Habis" => "DangerBrush",
            "Jatuh tempo hari ini" => "WarnBrush",
            _ => "PrimaryBrush"
        });
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Nama kategori → warna punggung buku yang konsisten.</summary>
public class CategoryBrushConverter : IValueConverter
{
    private static readonly string[] ColorsLight =
        { "#FF7F50", "#1A237E", "#B71C1C", "#EA754A", "#2D2D2D", "#8F96E8", "#6E6A5F", "#121752" };
    private static readonly string[] ColorsDark =
        { "#FF2D2D", "#E8C547", "#D4D4D4", "#F3793A", "#8F8F8F", "#4A4A4A", "#EA2929", "#B89B2E" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string ?? "";
        int hash = 0;
        foreach (var c in s) hash = (hash * 31 + c) & 0x7FFFFFFF;
        var colors = PerpusKu.Services.ThemeService.IsDark ? ColorsDark : ColorsLight;
        var color = (Color)ColorConverter.ConvertFromString(colors[hash % colors.Length]);
        return new SolidColorBrush(color);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Role → label singkat berwarna.</summary>
public class RoleToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) switch { "Admin" => "Administrator", "Pustakawan" => "Pustakawan", "Pengunjung" => "Pengunjung (hanya baca)", _ => value };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Kunci resource (mis. "SuccessBrush") → Brush yang mengikuti tema aktif.</summary>
public class KeyToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Res.Get(value as string ?? "PrimaryBrush");
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
