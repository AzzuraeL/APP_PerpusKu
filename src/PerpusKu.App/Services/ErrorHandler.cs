using Microsoft.Data.SqlClient;

namespace PerpusKu.Services;

/// <summary>Kesalahan aturan bisnis yang pesannya aman ditampilkan ke pengguna.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public static class ErrorHandler
{
    public static string ToFriendlyMessage(Exception ex) => ex switch
    {
        DomainException d => d.Message,
        UnauthorizedAccessException u => u.Message,
        SqlException s when s.Number >= 50000 => s.Message, // pesan dari THROW di stored procedure
        SqlException => "Tidak dapat mengakses database. Pastikan SQL Server LocalDB berjalan, lalu coba lagi.",
        TimeoutException => "Koneksi ke database terlalu lama. Silakan coba lagi.",
        _ => "Terjadi kesalahan tak terduga. Silakan coba lagi."
    };

    public static void Handle(Exception ex)
    {
        Log(ex);
        var message = ToFriendlyMessage(ex);
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null) return;
        if (dispatcher.CheckAccess()) ToastService.Show(message, ToastKind.Error);
        else dispatcher.Invoke(() => ToastService.Show(message, ToastKind.Error));
    }

    public static void Log(Exception ex)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PerpusKu", "logs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* jangan sampai logging menyebabkan error baru */ }
    }
}
