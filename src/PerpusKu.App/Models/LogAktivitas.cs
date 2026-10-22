namespace PerpusKu.Models;

public class LogAktivitas
{
    public string JenisAksi { get; set; }
    public string Deskripsi { get; set; }
    public string NamaUser { get; set; }
    public DateTime Waktu { get; set; }

    public string Glyph => JenisAksi switch
    {
        "Pinjam" => "book-open",
        "Kembali" => "check",
        "Perpanjang" => "refresh-ccw",
        "Tambah" => "plus",
        "Ubah" => "pencil",
        "Hapus" => "trash-2",
        _ => "info"
    };

    public string WaktuRelatif
    {
        get
        {
            var span = DateTime.Now - Waktu;
            if (span.TotalMinutes < 1) return "baru saja";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} menit lalu";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} jam lalu";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} hari lalu";
            return Waktu.ToString("dd MMM yyyy");
        }
    }
}
