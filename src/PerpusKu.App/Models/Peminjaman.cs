namespace PerpusKu.Models;

public class Peminjaman
{
    public int PeminjamanId { get; set; }
    public int BukuId { get; set; }
    public string KodeBuku { get; set; }
    public string Judul { get; set; }
    public int AnggotaId { get; set; }
    public string NomorAnggota { get; set; }
    public string NamaAnggota { get; set; }
    public DateTime TanggalPinjam { get; set; }
    public DateTime JatuhTempo { get; set; }
    public DateTime? TanggalKembali { get; set; }
    public string StatusDb { get; set; }
    public int JumlahPerpanjang { get; set; }
    public decimal Denda { get; set; }
    public int HariTerlambat { get; set; }

    public bool Aktif => StatusDb == "Dipinjam";

    /// <summary>Status yang ditampilkan di UI (memperhitungkan keterlambatan dan jatuh tempo hari ini).</summary>
    public string Status
    {
        get
        {
            if (!Aktif) return "Dikembalikan";
            if (HariTerlambat > 0) return "Terlambat";
            if (JatuhTempo.Date == DateTime.Today) return "Jatuh tempo hari ini";
            return "Dipinjam";
        }
    }

    public decimal DendaTampil => Aktif ? HariTerlambat * Services.SettingsCache.DendaPerHari : Denda;
    public string KeteranganWaktu => Aktif
        ? (HariTerlambat > 0 ? $"Terlambat {HariTerlambat} hari" : $"Tempo {JatuhTempo:dd MMM}")
        : $"Kembali {TanggalKembali:dd MMM}";
}
