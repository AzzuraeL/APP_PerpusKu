using System.Data;
using PerpusKu.Models;

namespace PerpusKu.Data.Repositories;

public class DashboardRepository
{
    public async Task<DashboardStat> GetStatsAsync()
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT
  (SELECT COUNT(*) FROM dbo.Buku WHERE IsActive = 1) AS JudulBuku,
  (SELECT ISNULL(SUM(StokTotal), 0) FROM dbo.Buku WHERE IsActive = 1) AS TotalEksemplar,
  (SELECT COUNT(*) FROM dbo.Anggota WHERE IsActive = 1) AS AnggotaAktif,
  (SELECT COUNT(*) FROM dbo.Peminjaman WHERE Status = N'Dipinjam') AS SedangDipinjam,
  (SELECT COUNT(*) FROM dbo.vw_PeminjamanDetail WHERE HariTerlambat > 0) AS Terlambat,
  (SELECT COUNT(*) FROM dbo.Peminjaman WHERE Status = N'Dipinjam' AND JatuhTempo = CAST(SYSDATETIME() AS DATE)) AS JatuhTempoHariIni,
  (SELECT ISNULL(SUM(Denda), 0) FROM dbo.Peminjaman
     WHERE Status = N'Dikembalikan'
       AND YEAR(TanggalKembali) = YEAR(SYSDATETIME()) AND MONTH(TanggalKembali) = MONTH(SYSDATETIME())) AS DendaBulanIni";
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new DashboardStat
        {
            JudulBuku = r.Int("JudulBuku"),
            TotalEksemplar = r.Int("TotalEksemplar"),
            AnggotaAktif = r.Int("AnggotaAktif"),
            SedangDipinjam = r.Int("SedangDipinjam"),
            Terlambat = r.Int("Terlambat"),
            JatuhTempoHariIni = r.Int("JatuhTempoHariIni"),
            DendaBulanIni = r.Dec("DendaBulanIni")
        };
    }

    public async Task<List<TitikData>> GetBukuPerKategoriAsync()
    {
        var list = new List<TitikData>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Nama, JumlahBuku FROM dbo.vw_BukuPerKategori WHERE JumlahBuku > 0 ORDER BY JumlahBuku DESC, Nama";
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new TitikData { Label = r.Str("Nama"), Nilai = r.Int("JumlahBuku") });
        return list;
    }

    /// <summary>Jumlah peminjaman baru per hari selama N hari terakhir (termasuk hari ini), hari kosong diisi 0.</summary>
    public async Task<List<TitikData>> GetTrenPinjamAsync(int hari = 7)
    {
        var dari = DateTime.Today.AddDays(-(hari - 1));
        var map = new Dictionary<DateTime, int>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TanggalPinjam, COUNT(*) AS Jumlah FROM dbo.Peminjaman WHERE TanggalPinjam >= @dari GROUP BY TanggalPinjam";
        cmd.Add("@dari", SqlDbType.Date, dari);
        await using (var r = await cmd.ExecuteReaderAsync())
            while (await r.ReadAsync()) map[r.Date("TanggalPinjam").Date] = r.Int("Jumlah");

        var list = new List<TitikData>();
        for (int i = 0; i < hari; i++)
        {
            var d = dari.AddDays(i);
            list.Add(new TitikData { Label = d.ToString("ddd dd"), Nilai = map.TryGetValue(d, out var n) ? n : 0 });
        }
        return list;
    }

    /// <summary>Pinjaman yang terlambat atau jatuh tempo dalam 2 hari ke depan.</summary>
    public async Task<List<Peminjaman>> GetPerluPerhatianAsync(int top = 6)
    {
        var list = new List<Peminjaman>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT TOP (@n) PeminjamanId, BukuId, KodeBuku, Judul, AnggotaId, NomorAnggota, NamaAnggota,
       TanggalPinjam, JatuhTempo, TanggalKembali, Status, JumlahPerpanjang, Denda, HariTerlambat
FROM dbo.vw_PeminjamanDetail
WHERE Status = N'Dipinjam' AND JatuhTempo <= DATEADD(DAY, 2, CAST(SYSDATETIME() AS DATE))
ORDER BY JatuhTempo, PeminjamanId";
        cmd.Add("@n", SqlDbType.Int, top);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Peminjaman
            {
                PeminjamanId = r.Int("PeminjamanId"),
                KodeBuku = r.Str("KodeBuku"),
                Judul = r.Str("Judul"),
                NomorAnggota = r.Str("NomorAnggota"),
                NamaAnggota = r.Str("NamaAnggota"),
                TanggalPinjam = r.Date("TanggalPinjam"),
                JatuhTempo = r.Date("JatuhTempo"),
                StatusDb = r.Str("Status"),
                HariTerlambat = r.Int("HariTerlambat")
            });
        return list;
    }

    public async Task<List<LogAktivitas>> GetAktivitasAsync(int top = 6)
    {
        var list = new List<LogAktivitas>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT TOP (@n) l.JenisAksi, l.Deskripsi, l.Waktu, u.NamaLengkap
FROM dbo.LogAktivitas l
LEFT JOIN dbo.Users u ON u.UserId = l.UserId
ORDER BY l.Waktu DESC, l.LogId DESC";
        cmd.Add("@n", SqlDbType.Int, top);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new LogAktivitas
            {
                JenisAksi = r.Str("JenisAksi"),
                Deskripsi = r.Str("Deskripsi"),
                NamaUser = r.Str("NamaLengkap"),
                Waktu = r.Date("Waktu")
            });
        return list;
    }

    public async Task<RingkasanLaporan> GetRingkasanAsync(DateTime dari, DateTime sampai)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*) AS TotalPinjam,
       ISNULL(SUM(CASE WHEN Status = N'Dikembalikan' THEN 1 ELSE 0 END), 0) AS Dikembalikan,
       ISNULL(SUM(CASE WHEN Status = N'Dipinjam' THEN 1 ELSE 0 END), 0) AS MasihDipinjam,
       ISNULL(SUM(Denda), 0) AS TotalDenda
FROM dbo.Peminjaman
WHERE TanggalPinjam BETWEEN @dari AND @sampai";
        cmd.Add("@dari", SqlDbType.Date, dari.Date);
        cmd.Add("@sampai", SqlDbType.Date, sampai.Date);
        await using var r = await cmd.ExecuteReaderAsync();
        await r.ReadAsync();
        return new RingkasanLaporan
        {
            TotalPinjam = r.Int("TotalPinjam"),
            Dikembalikan = r.Int("Dikembalikan"),
            MasihDipinjam = r.Int("MasihDipinjam"),
            TotalDenda = r.Dec("TotalDenda")
        };
    }

    public async Task<List<BukuPopuler>> GetBukuPopulerAsync(DateTime dari, DateTime sampai, int top = 5)
    {
        var list = new List<BukuPopuler>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT TOP (@n) b.KodeBuku, b.Judul, b.Penulis, COUNT(*) AS Jumlah
FROM dbo.Peminjaman p
JOIN dbo.Buku b ON b.BukuId = p.BukuId
WHERE p.TanggalPinjam BETWEEN @dari AND @sampai
GROUP BY b.KodeBuku, b.Judul, b.Penulis
ORDER BY COUNT(*) DESC, b.Judul";
        cmd.Add("@n", SqlDbType.Int, top);
        cmd.Add("@dari", SqlDbType.Date, dari.Date);
        cmd.Add("@sampai", SqlDbType.Date, sampai.Date);
        await using var r = await cmd.ExecuteReaderAsync();
        int rank = 1;
        while (await r.ReadAsync())
            list.Add(new BukuPopuler { Peringkat = rank++, KodeBuku = r.Str("KodeBuku"), Judul = r.Str("Judul"), Penulis = r.Str("Penulis"), JumlahPinjam = r.Int("Jumlah") });
        return list;
    }
}
