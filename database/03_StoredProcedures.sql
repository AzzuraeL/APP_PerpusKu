-- PerpusKu - 03: aturan sirkulasi sebagai stored procedure (transaksional)
USE PerpusKu_DB;
GO

CREATE OR ALTER FUNCTION dbo.fn_Setting(@Kunci NVARCHAR(50), @Default INT)
RETURNS INT
AS
BEGIN
    DECLARE @v INT;
    SELECT @v = TRY_CAST(Nilai AS INT) FROM dbo.Pengaturan WHERE Kunci = @Kunci;
    RETURN COALESCE(@v, @Default);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_PinjamBuku
    @KodeBuku NVARCHAR(20),
    @NomorAnggota NVARCHAR(20),
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BukuId INT, @Judul NVARCHAR(200), @Tersedia INT, @AnggotaId INT, @Nama NVARCHAR(120);
    DECLARE @Lama INT = dbo.fn_Setting(N'LamaPinjamHari', 7);
    DECLARE @Maks INT = dbo.fn_Setting(N'MaksPinjam', 3);
    DECLARE @Hari DATE = CAST(SYSDATETIME() AS DATE);
    DECLARE @Tempo DATE = DATEADD(DAY, @Lama, @Hari);

    BEGIN TRAN;

    SELECT @BukuId = BukuId, @Judul = Judul, @Tersedia = Tersedia
    FROM dbo.Buku WITH (UPDLOCK, ROWLOCK)
    WHERE KodeBuku = @KodeBuku AND IsActive = 1;

    IF @BukuId IS NULL THROW 50001, N'Kode buku tidak ditemukan.', 1;
    IF @Tersedia <= 0  THROW 50002, N'Stok buku ini sedang habis.', 1;

    SELECT @AnggotaId = AnggotaId, @Nama = Nama
    FROM dbo.Anggota WHERE NomorAnggota = @NomorAnggota AND IsActive = 1;

    IF @AnggotaId IS NULL THROW 50003, N'Nomor anggota tidak ditemukan atau tidak aktif.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Peminjaman WHERE AnggotaId = @AnggotaId AND Status = N'Dipinjam' AND JatuhTempo < @Hari)
        THROW 50004, N'Anggota masih memiliki buku yang terlambat dikembalikan.', 1;

    IF (SELECT COUNT(*) FROM dbo.Peminjaman WHERE AnggotaId = @AnggotaId AND Status = N'Dipinjam') >= @Maks
        THROW 50005, N'Batas maksimal peminjaman anggota sudah tercapai.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Peminjaman WHERE AnggotaId = @AnggotaId AND BukuId = @BukuId AND Status = N'Dipinjam')
        THROW 50006, N'Anggota sedang meminjam judul yang sama.', 1;

    INSERT dbo.Peminjaman (BukuId, AnggotaId, UserId, TanggalPinjam, JatuhTempo)
    VALUES (@BukuId, @AnggotaId, @UserId, @Hari, @Tempo);

    UPDATE dbo.Buku SET Tersedia = Tersedia - 1 WHERE BukuId = @BukuId;

    INSERT dbo.LogAktivitas (UserId, JenisAksi, Entitas, Deskripsi)
    VALUES (@UserId, N'Pinjam', N'Peminjaman', @Nama + N' meminjam "' + @Judul + N'"');

    COMMIT;
    SELECT @Tempo AS JatuhTempo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_KembalikanBuku
    @PeminjamanId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BukuId INT, @Tempo DATE, @Judul NVARCHAR(200), @Nama NVARCHAR(120);
    DECLARE @PerHari INT = dbo.fn_Setting(N'DendaPerHari', 1000);
    DECLARE @Hari DATE = CAST(SYSDATETIME() AS DATE);
    DECLARE @Telat INT, @Denda DECIMAL(12,2);

    BEGIN TRAN;

    SELECT @BukuId = p.BukuId, @Tempo = p.JatuhTempo, @Judul = b.Judul, @Nama = a.Nama
    FROM dbo.Peminjaman p
    JOIN dbo.Buku b ON b.BukuId = p.BukuId
    JOIN dbo.Anggota a ON a.AnggotaId = p.AnggotaId
    WHERE p.PeminjamanId = @PeminjamanId AND p.Status = N'Dipinjam';

    IF @BukuId IS NULL THROW 50011, N'Data peminjaman aktif tidak ditemukan.', 1;

    SET @Telat = CASE WHEN @Hari > @Tempo THEN DATEDIFF(DAY, @Tempo, @Hari) ELSE 0 END;
    SET @Denda = @Telat * @PerHari;

    UPDATE dbo.Peminjaman
    SET Status = N'Dikembalikan', TanggalKembali = @Hari, Denda = @Denda
    WHERE PeminjamanId = @PeminjamanId;

    UPDATE dbo.Buku SET Tersedia = Tersedia + 1 WHERE BukuId = @BukuId;

    INSERT dbo.LogAktivitas (UserId, JenisAksi, Entitas, Deskripsi)
    VALUES (@UserId, N'Kembali', N'Peminjaman', @Nama + N' mengembalikan "' + @Judul + N'"');

    COMMIT;
    SELECT @Denda AS Denda, @Telat AS HariTerlambat;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_PerpanjangBuku
    @PeminjamanId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Tempo DATE, @Kali INT, @Judul NVARCHAR(200), @Nama NVARCHAR(120);
    DECLARE @Lama INT = dbo.fn_Setting(N'LamaPinjamHari', 7);
    DECLARE @MaksKali INT = dbo.fn_Setting(N'MaksPerpanjang', 1);
    DECLARE @Hari DATE = CAST(SYSDATETIME() AS DATE);

    BEGIN TRAN;

    SELECT @Tempo = p.JatuhTempo, @Kali = p.JumlahPerpanjang, @Judul = b.Judul, @Nama = a.Nama
    FROM dbo.Peminjaman p
    JOIN dbo.Buku b ON b.BukuId = p.BukuId
    JOIN dbo.Anggota a ON a.AnggotaId = p.AnggotaId
    WHERE p.PeminjamanId = @PeminjamanId AND p.Status = N'Dipinjam';

    IF @Tempo IS NULL THROW 50021, N'Data peminjaman aktif tidak ditemukan.', 1;
    IF @Tempo < @Hari THROW 50022, N'Buku yang sudah terlambat tidak dapat diperpanjang.', 1;
    IF @Kali >= @MaksKali THROW 50023, N'Batas perpanjangan sudah tercapai.', 1;

    UPDATE dbo.Peminjaman
    SET JatuhTempo = DATEADD(DAY, @Lama, JatuhTempo), JumlahPerpanjang = JumlahPerpanjang + 1
    WHERE PeminjamanId = @PeminjamanId;

    INSERT dbo.LogAktivitas (UserId, JenisAksi, Entitas, Deskripsi)
    VALUES (@UserId, N'Perpanjang', N'Peminjaman', @Nama + N' memperpanjang "' + @Judul + N'"');

    COMMIT;
    SELECT DATEADD(DAY, @Lama, @Tempo) AS JatuhTempoBaru;
END;
GO
