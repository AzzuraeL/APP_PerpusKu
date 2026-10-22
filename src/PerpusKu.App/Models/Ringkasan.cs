namespace PerpusKu.Models;

public class DashboardStat
{
    public int JudulBuku { get; set; }
    public int TotalEksemplar { get; set; }
    public int AnggotaAktif { get; set; }
    public int SedangDipinjam { get; set; }
    public int Terlambat { get; set; }
    public int JatuhTempoHariIni { get; set; }
    public decimal DendaBulanIni { get; set; }
}

public class TitikData
{
    public string Label { get; set; }
    public int Nilai { get; set; }
}

public class BukuPopuler
{
    public int Peringkat { get; set; }
    public string KodeBuku { get; set; }
    public string Judul { get; set; }
    public string Penulis { get; set; }
    public int JumlahPinjam { get; set; }
}

public class RingkasanLaporan
{
    public int TotalPinjam { get; set; }
    public int Dikembalikan { get; set; }
    public int MasihDipinjam { get; set; }
    public decimal TotalDenda { get; set; }
}

public class PengaturanItem
{
    public string Kunci { get; set; }
    public string Nilai { get; set; }
    public string Label { get; set; }
}
