using PerpusKu.Models;

namespace PerpusKu.Services;

public enum Permission
{
    BukuWrite,
    AnggotaWrite,
    Sirkulasi,
    Laporan,
    KelolaPengaturan
}

/// <summary>Menyimpan pengguna yang sedang login dan matriks hak akses per peran.</summary>
public static class SessionService
{
    public static User Current { get; private set; }
    public static bool IsLoggedIn => Current != null;

    public static void SignIn(User user) => Current = user;
    public static void SignOut() => Current = null;

    public static bool Can(Permission permission)
    {
        if (Current == null) return false;
        return Current.Role switch
        {
            "Admin" => true,
            "Pustakawan" => permission != Permission.KelolaPengaturan,
            _ => false // Pengunjung: hanya-baca
        };
    }

    /// <summary>Dipanggil di batas repository/service agar aksi tetap ditolak walaupun UI dimanipulasi.</summary>
    public static void Require(Permission permission)
    {
        if (!Can(permission))
            throw new UnauthorizedAccessException("Anda tidak memiliki izin untuk melakukan aksi ini.");
    }

    public static int? CurrentUserId => Current?.UserId;
}
