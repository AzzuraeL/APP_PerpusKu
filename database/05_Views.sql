-- PerpusKu - 05: view untuk riwayat, dashboard, dan laporan
USE PerpusKu_DB;
GO

CREATE OR ALTER VIEW dbo.vw_PeminjamanDetail
AS
SELECT p.PeminjamanId, p.BukuId, b.KodeBuku, b.Judul,
       p.AnggotaId, a.NomorAnggota, a.Nama AS NamaAnggota,
       p.TanggalPinjam, p.JatuhTempo, p.TanggalKembali, p.Status,
       p.JumlahPerpanjang, p.Denda,
       CASE WHEN p.Status = N'Dipinjam' AND p.JatuhTempo < CAST(SYSDATETIME() AS DATE)
            THEN DATEDIFF(DAY, p.JatuhTempo, CAST(SYSDATETIME() AS DATE)) ELSE 0 END AS HariTerlambat
FROM dbo.Peminjaman p
JOIN dbo.Buku b ON b.BukuId = p.BukuId
JOIN dbo.Anggota a ON a.AnggotaId = p.AnggotaId;
GO

CREATE OR ALTER VIEW dbo.vw_BukuPerKategori
AS
SELECT k.KategoriId, k.Nama, COUNT(b.BukuId) AS JumlahBuku
FROM dbo.Kategori k
LEFT JOIN dbo.Buku b ON b.KategoriId = k.KategoriId AND b.IsActive = 1
GROUP BY k.KategoriId, k.Nama;
GO
