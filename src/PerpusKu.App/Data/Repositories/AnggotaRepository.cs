using System.Data;
using PerpusKu.Models;
using PerpusKu.Services;

namespace PerpusKu.Data.Repositories;

public class AnggotaRepository
{
    private const string SelectSql = @"
SELECT a.AnggotaId, a.NomorAnggota, a.Nama, a.Tipe, a.Email, a.Telepon, a.TanggalDaftar,
       (SELECT COUNT(*) FROM dbo.Peminjaman p WHERE p.AnggotaId = a.AnggotaId AND p.Status = N'Dipinjam') AS SedangPinjam
FROM dbo.Anggota a";

    private static Anggota Map(Microsoft.Data.SqlClient.SqlDataReader r) => new()
    {
        AnggotaId = r.Int("AnggotaId"),
        NomorAnggota = r.Str("NomorAnggota"),
        Nama = r.Str("Nama"),
        Tipe = r.Str("Tipe"),
        Email = r.Str("Email"),
        Telepon = r.Str("Telepon"),
        TanggalDaftar = r.Date("TanggalDaftar"),
        SedangPinjam = r.Int("SedangPinjam")
    };

    public async Task<List<Anggota>> GetAllAsync(string search, string tipe)
    {
        var list = new List<Anggota>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + @"
WHERE a.IsActive = 1
  AND (a.Nama LIKE @q OR a.NomorAnggota LIKE @q OR a.Email LIKE @q)
  AND (@tipe IS NULL OR a.Tipe = @tipe)
ORDER BY a.Nama";
        cmd.Add("@q", SqlDbType.NVarChar, ReaderExtensions.Like(search));
        cmd.Add("@tipe", SqlDbType.NVarChar, string.IsNullOrEmpty(tipe) || tipe == "Semua" ? null : tipe);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<Anggota> GetByNomorAsync(string nomor)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + " WHERE a.IsActive = 1 AND a.NomorAnggota = @n";
        cmd.Add("@n", SqlDbType.NVarChar, (nomor ?? "").Trim());
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<string> NextNomorAsync()
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Anggota";
        int n = Convert.ToInt32(await cmd.ExecuteScalarAsync()) + 1;
        return $"A-{DateTime.Now:yy}-{n:000}";
    }

    public async Task<bool> NomorExistsAsync(string nomor, int excludeId)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Anggota WHERE NomorAnggota = @n AND AnggotaId <> @id";
        cmd.Add("@n", SqlDbType.NVarChar, nomor.Trim());
        cmd.Add("@id", SqlDbType.Int, excludeId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task InsertAsync(Anggota a)
    {
        SessionService.Require(Permission.AnggotaWrite);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT dbo.Anggota (NomorAnggota, Nama, Tipe, Email, Telepon)
                            VALUES (@no, @nama, @tipe, @email, @telp)";
        Fill(cmd, a);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Anggota a)
    {
        SessionService.Require(Permission.AnggotaWrite);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE dbo.Anggota SET NomorAnggota=@no, Nama=@nama, Tipe=@tipe, Email=@email, Telepon=@telp
                            WHERE AnggotaId=@id";
        Fill(cmd, a);
        cmd.Add("@id", SqlDbType.Int, a.AnggotaId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int anggotaId)
    {
        SessionService.Require(Permission.AnggotaWrite);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.Peminjaman WHERE AnggotaId = @id AND Status = N'Dipinjam')
    THROW 50041, N'Anggota tidak dapat dihapus karena masih memiliki pinjaman aktif.', 1;
UPDATE dbo.Anggota SET IsActive = 0 WHERE AnggotaId = @id;";
        cmd.Add("@id", SqlDbType.Int, anggotaId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void Fill(Microsoft.Data.SqlClient.SqlCommand cmd, Anggota a)
    {
        cmd.Add("@no", SqlDbType.NVarChar, a.NomorAnggota.Trim());
        cmd.Add("@nama", SqlDbType.NVarChar, a.Nama.Trim());
        cmd.Add("@tipe", SqlDbType.NVarChar, a.Tipe);
        cmd.Add("@email", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(a.Email) ? null : a.Email.Trim());
        cmd.Add("@telp", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(a.Telepon) ? null : a.Telepon.Trim());
    }
}
