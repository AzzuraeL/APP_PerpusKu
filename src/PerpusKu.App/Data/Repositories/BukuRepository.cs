using System.Data;
using PerpusKu.Models;
using PerpusKu.Services;

namespace PerpusKu.Data.Repositories;

public class BukuRepository
{
    private const string SelectSql = @"
SELECT b.BukuId, b.KodeBuku, b.Judul, b.Penulis, b.Penerbit, b.Tahun, b.KategoriId,
       k.Nama AS KategoriNama, b.StokTotal, b.Tersedia, b.Rak
FROM dbo.Buku b
JOIN dbo.Kategori k ON k.KategoriId = b.KategoriId";

    private static Buku Map(Microsoft.Data.SqlClient.SqlDataReader r) => new()
    {
        BukuId = r.Int("BukuId"),
        KodeBuku = r.Str("KodeBuku"),
        Judul = r.Str("Judul"),
        Penulis = r.Str("Penulis"),
        Penerbit = r.Str("Penerbit"),
        Tahun = r.IntN("Tahun"),
        KategoriId = r.Int("KategoriId"),
        KategoriNama = r.Str("KategoriNama"),
        StokTotal = r.Int("StokTotal"),
        Tersedia = r.Int("Tersedia"),
        Rak = r.Str("Rak")
    };

    public async Task<List<Buku>> GetAllAsync(string search, int? kategoriId, bool hanyaTersedia)
    {
        var list = new List<Buku>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + @"
WHERE b.IsActive = 1
  AND (b.Judul LIKE @q OR b.Penulis LIKE @q OR b.KodeBuku LIKE @q)
  AND (@kat IS NULL OR b.KategoriId = @kat)
  AND (@avail = 0 OR b.Tersedia > 0)
ORDER BY b.Judul";
        cmd.Add("@q", SqlDbType.NVarChar, ReaderExtensions.Like(search));
        cmd.Add("@kat", SqlDbType.Int, kategoriId);
        cmd.Add("@avail", SqlDbType.Bit, hanyaTersedia);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<Buku> GetByKodeAsync(string kode)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + " WHERE b.IsActive = 1 AND b.KodeBuku = @k";
        cmd.Add("@k", SqlDbType.NVarChar, (kode ?? "").Trim());
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task<bool> KodeExistsAsync(string kode, int excludeId)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.Buku WHERE KodeBuku = @k AND BukuId <> @id";
        cmd.Add("@k", SqlDbType.NVarChar, kode.Trim());
        cmd.Add("@id", SqlDbType.Int, excludeId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task InsertAsync(Buku b)
    {
        SessionService.Require(Permission.BukuWrite);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT dbo.Buku (KodeBuku, Judul, Penulis, Penerbit, Tahun, KategoriId, StokTotal, Tersedia, Rak)
                            VALUES (@kode, @judul, @penulis, @penerbit, @tahun, @kat, @stok, @stok, @rak)";
        Fill(cmd, b);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Buku b)
    {
        SessionService.Require(Permission.BukuWrite);
        await using var conn = await Db.OpenAsync();
        await using var tx = (Microsoft.Data.SqlClient.SqlTransaction)await conn.BeginTransactionAsync();
        try
        {
            // stok total tidak boleh lebih kecil dari jumlah yang sedang dipinjam
            int dipinjam;
            await using (var c1 = conn.CreateCommand())
            {
                c1.Transaction = tx;
                c1.CommandText = "SELECT COUNT(*) FROM dbo.Peminjaman WHERE BukuId = @id AND Status = N'Dipinjam'";
                c1.Add("@id", SqlDbType.Int, b.BukuId);
                dipinjam = Convert.ToInt32(await c1.ExecuteScalarAsync());
            }
            if (b.StokTotal < dipinjam)
                throw new DomainException($"Stok total tidak boleh kurang dari jumlah yang sedang dipinjam ({dipinjam}).");

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"UPDATE dbo.Buku SET KodeBuku=@kode, Judul=@judul, Penulis=@penulis, Penerbit=@penerbit,
                                Tahun=@tahun, KategoriId=@kat, StokTotal=@stok, Tersedia=@stok - @dipinjam, Rak=@rak
                                WHERE BukuId=@id";
            Fill(cmd, b);
            cmd.Add("@id", SqlDbType.Int, b.BukuId);
            cmd.Add("@dipinjam", SqlDbType.Int, dipinjam);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int bukuId)
    {
        SessionService.Require(Permission.BukuWrite);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.Peminjaman WHERE BukuId = @id AND Status = N'Dipinjam')
    THROW 50031, N'Buku tidak dapat dihapus karena masih dipinjam.', 1;
UPDATE dbo.Buku SET IsActive = 0 WHERE BukuId = @id;";
        cmd.Add("@id", SqlDbType.Int, bukuId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void Fill(Microsoft.Data.SqlClient.SqlCommand cmd, Buku b)
    {
        cmd.Add("@kode", SqlDbType.NVarChar, b.KodeBuku.Trim());
        cmd.Add("@judul", SqlDbType.NVarChar, b.Judul.Trim());
        cmd.Add("@penulis", SqlDbType.NVarChar, b.Penulis.Trim());
        cmd.Add("@penerbit", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(b.Penerbit) ? null : b.Penerbit.Trim());
        cmd.Add("@tahun", SqlDbType.Int, b.Tahun);
        cmd.Add("@kat", SqlDbType.Int, b.KategoriId);
        cmd.Add("@stok", SqlDbType.Int, b.StokTotal);
        cmd.Add("@rak", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(b.Rak) ? null : b.Rak.Trim());
    }
}
