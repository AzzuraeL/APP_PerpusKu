-- PerpusKu - 02: tabel, relasi, constraint, index
USE PerpusKu_DB;
GO

IF OBJECT_ID(N'dbo.Users') IS NULL
CREATE TABLE dbo.Users (
    UserId       INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    PasswordSalt NVARCHAR(100) NOT NULL,
    NamaLengkap  NVARCHAR(100) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Pustakawan', N'Pengunjung')),
    IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt    DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
);
GO

IF OBJECT_ID(N'dbo.Kategori') IS NULL
CREATE TABLE dbo.Kategori (
    KategoriId INT IDENTITY(1,1) CONSTRAINT PK_Kategori PRIMARY KEY,
    Nama       NVARCHAR(60) NOT NULL CONSTRAINT UQ_Kategori_Nama UNIQUE
);
GO

IF OBJECT_ID(N'dbo.Buku') IS NULL
CREATE TABLE dbo.Buku (
    BukuId     INT IDENTITY(1,1) CONSTRAINT PK_Buku PRIMARY KEY,
    KodeBuku   NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Buku_Kode UNIQUE,
    Judul      NVARCHAR(200) NOT NULL,
    Penulis    NVARCHAR(120) NOT NULL,
    Penerbit   NVARCHAR(120) NULL,
    Tahun      INT NULL CONSTRAINT CK_Buku_Tahun CHECK (Tahun IS NULL OR Tahun BETWEEN 1500 AND 2100),
    KategoriId INT NOT NULL CONSTRAINT FK_Buku_Kategori REFERENCES dbo.Kategori(KategoriId),
    StokTotal  INT NOT NULL CONSTRAINT CK_Buku_StokTotal CHECK (StokTotal >= 0),
    Tersedia   INT NOT NULL CONSTRAINT CK_Buku_Tersedia CHECK (Tersedia >= 0),
    Rak        NVARCHAR(20) NULL,
    IsActive   BIT NOT NULL CONSTRAINT DF_Buku_IsActive DEFAULT 1,
    CONSTRAINT CK_Buku_Stok CHECK (Tersedia <= StokTotal)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Buku_Judul') CREATE INDEX IX_Buku_Judul ON dbo.Buku(Judul);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Buku_Kategori') CREATE INDEX IX_Buku_Kategori ON dbo.Buku(KategoriId);
GO

IF OBJECT_ID(N'dbo.Anggota') IS NULL
CREATE TABLE dbo.Anggota (
    AnggotaId      INT IDENTITY(1,1) CONSTRAINT PK_Anggota PRIMARY KEY,
    NomorAnggota   NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Anggota_Nomor UNIQUE,
    Nama           NVARCHAR(120) NOT NULL,
    Tipe           NVARCHAR(20)  NOT NULL CONSTRAINT CK_Anggota_Tipe CHECK (Tipe IN (N'Mahasiswa', N'Dosen', N'Staf')),
    Email          NVARCHAR(120) NULL,
    Telepon        NVARCHAR(30)  NULL,
    TanggalDaftar  DATE NOT NULL CONSTRAINT DF_Anggota_Daftar DEFAULT CAST(SYSDATETIME() AS DATE),
    IsActive       BIT NOT NULL CONSTRAINT DF_Anggota_IsActive DEFAULT 1
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Anggota_Nama') CREATE INDEX IX_Anggota_Nama ON dbo.Anggota(Nama);
GO

IF OBJECT_ID(N'dbo.Peminjaman') IS NULL
CREATE TABLE dbo.Peminjaman (
    PeminjamanId     INT IDENTITY(1,1) CONSTRAINT PK_Peminjaman PRIMARY KEY,
    BukuId           INT NOT NULL CONSTRAINT FK_Pinjam_Buku REFERENCES dbo.Buku(BukuId),
    AnggotaId        INT NOT NULL CONSTRAINT FK_Pinjam_Anggota REFERENCES dbo.Anggota(AnggotaId),
    UserId           INT NULL CONSTRAINT FK_Pinjam_User REFERENCES dbo.Users(UserId),
    TanggalPinjam    DATE NOT NULL,
    JatuhTempo       DATE NOT NULL,
    TanggalKembali   DATE NULL,
    Status           NVARCHAR(20) NOT NULL CONSTRAINT DF_Pinjam_Status DEFAULT N'Dipinjam'
                     CONSTRAINT CK_Pinjam_Status CHECK (Status IN (N'Dipinjam', N'Dikembalikan')),
    JumlahPerpanjang INT NOT NULL CONSTRAINT DF_Pinjam_Perpanjang DEFAULT 0,
    Denda            DECIMAL(12,2) NOT NULL CONSTRAINT DF_Pinjam_Denda DEFAULT 0,
    CONSTRAINT CK_Pinjam_Tanggal CHECK (JatuhTempo >= TanggalPinjam)
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pinjam_Status') CREATE INDEX IX_Pinjam_Status ON dbo.Peminjaman(Status, JatuhTempo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pinjam_Anggota') CREATE INDEX IX_Pinjam_Anggota ON dbo.Peminjaman(AnggotaId, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pinjam_Buku') CREATE INDEX IX_Pinjam_Buku ON dbo.Peminjaman(BukuId, Status);
GO

IF OBJECT_ID(N'dbo.Pengaturan') IS NULL
CREATE TABLE dbo.Pengaturan (
    Kunci NVARCHAR(50)  NOT NULL CONSTRAINT PK_Pengaturan PRIMARY KEY,
    Nilai NVARCHAR(100) NOT NULL,
    Label NVARCHAR(100) NOT NULL
);
GO

IF OBJECT_ID(N'dbo.LogAktivitas') IS NULL
CREATE TABLE dbo.LogAktivitas (
    LogId      INT IDENTITY(1,1) CONSTRAINT PK_Log PRIMARY KEY,
    UserId     INT NULL CONSTRAINT FK_Log_User REFERENCES dbo.Users(UserId),
    JenisAksi  NVARCHAR(30)  NOT NULL,
    Entitas    NVARCHAR(30)  NOT NULL,
    Deskripsi  NVARCHAR(250) NOT NULL,
    Waktu      DATETIME2 NOT NULL CONSTRAINT DF_Log_Waktu DEFAULT SYSDATETIME()
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Log_Waktu') CREATE INDEX IX_Log_Waktu ON dbo.LogAktivitas(Waktu DESC);
GO
