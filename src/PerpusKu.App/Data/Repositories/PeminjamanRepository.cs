using System.Data;
using Microsoft.Data.SqlClient;
using PerpusKu.Models;
using PerpusKu.Services;

namespace PerpusKu.Data.Repositories;

public class PeminjamanRepository
{
    private const string SelectSql = @"
SELECT PeminjamanId, BukuId, KodeBuku, Judul, AnggotaId, NomorAnggota, NamaAnggota,
       TanggalPinjam, JatuhTempo, TanggalKembali, Status, JumlahPerpanjang, Denda, HariTerlambat
FROM dbo.vw_PeminjamanDetail";

    private static Peminjaman Map(SqlDataReader r) => new()
    {
        PeminjamanId = r.Int("PeminjamanId"),
        BukuId = r.Int("BukuId"),
        KodeBuku = r.Str("KodeBuku"),
        Judul = r.Str("Judul"),
        AnggotaId = r.Int("AnggotaId"),
        NomorAnggota = r.Str("NomorAnggota"),
        NamaAnggota = r.Str("NamaAnggota"),
        TanggalPinjam = r.Date("TanggalPinjam"),
        JatuhTempo = r.Date("JatuhTempo"),
        TanggalKembali = r.DateN("TanggalKembali"),
        StatusDb = r.Str("Status"),
        JumlahPerpanjang = r.Int("JumlahPerpanjang"),
        Denda = r.Dec("Denda"),
        HariTerlambat = r.Int("HariTerlambat")
    };

    /// <param name="status">Semua | Dipinjam | Terlambat | Dikembalikan</param>
    public async Task<List<Peminjaman>> QueryAsync(string search, string status, DateTime? dari = null, DateTime? sampai = null)
    {
        var list = new List<Peminjaman>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + @"
WHERE (Judul LIKE @q OR NamaAnggota LIKE @q OR KodeBuku LIKE @q OR NomorAnggota LIKE @q)
  AND (@s = N'Semua'
       OR (@s = N'Dipinjam' AND Status = N'Dipinjam')
       OR (@s = N'Terlambat' AND HariTerlambat > 0)
       OR (@s = N'Dikembalikan' AND Status = N'Dikembalikan'))
  AND (@dari IS NULL OR TanggalPinjam >= @dari)
  AND (@sampai IS NULL OR TanggalPinjam <= @sampai)
ORDER BY TanggalPinjam DESC, PeminjamanId DESC";
        cmd.Add("@q", SqlDbType.NVarChar, ReaderExtensions.Like(search));
        cmd.Add("@s", SqlDbType.NVarChar, string.IsNullOrEmpty(status) ? "Semua" : status);
        cmd.Add("@dari", SqlDbType.Date, dari?.Date);
        cmd.Add("@sampai", SqlDbType.Date, sampai?.Date);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    /// <summary>Pinjaman aktif yang cocok dengan kata kunci (kode buku, judul, nomor/nama anggota).</summary>
    public async Task<List<Peminjaman>> GetAktifAsync(string keyword)
    {
        var list = new List<Peminjaman>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = SelectSql + @"
WHERE Status = N'Dipinjam'
  AND (Judul LIKE @q OR NamaAnggota LIKE @q OR KodeBuku LIKE @q OR NomorAnggota LIKE @q)
ORDER BY JatuhTempo, PeminjamanId";
        cmd.Add("@q", SqlDbType.NVarChar, ReaderExtensions.Like(keyword));
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<DateTime> PinjamAsync(string kodeBuku, string nomorAnggota)
    {
        SessionService.Require(Permission.Sirkulasi);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.sp_PinjamBuku";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Add("@KodeBuku", SqlDbType.NVarChar, kodeBuku.Trim());
        cmd.Add("@NomorAnggota", SqlDbType.NVarChar, nomorAnggota.Trim());
        cmd.Add("@UserId", SqlDbType.Int, SessionService.CurrentUserId);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return r.Date("JatuhTempo");
    }

    public async Task<(decimal Denda, int HariTerlambat)> KembalikanAsync(int peminjamanId)
    {
        SessionService.Require(Permission.Sirkulasi);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.sp_KembalikanBuku";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Add("@PeminjamanId", SqlDbType.Int, peminjamanId);
        cmd.Add("@UserId", SqlDbType.Int, SessionService.CurrentUserId);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return (r.Dec("Denda"), r.Int("HariTerlambat"));
    }

    public async Task<DateTime> PerpanjangAsync(int peminjamanId)
    {
        SessionService.Require(Permission.Sirkulasi);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "dbo.sp_PerpanjangBuku";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Add("@PeminjamanId", SqlDbType.Int, peminjamanId);
        cmd.Add("@UserId", SqlDbType.Int, SessionService.CurrentUserId);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return r.Date("JatuhTempoBaru");
    }
}
