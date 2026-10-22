namespace PerpusKu.Models;

public class Buku
{
    public int BukuId { get; set; }
    public string KodeBuku { get; set; }
    public string Judul { get; set; }
    public string Penulis { get; set; }
    public string Penerbit { get; set; }
    public int? Tahun { get; set; }
    public int KategoriId { get; set; }
    public string KategoriNama { get; set; }
    public int StokTotal { get; set; }
    public int Tersedia { get; set; }
    public string Rak { get; set; }

    public string StokText => $"{Tersedia} / {StokTotal}";
    public string Status => Tersedia > 0 ? "Tersedia" : "Habis";
    public string PenerbitTahun => Tahun.HasValue ? $"{Penerbit} · {Tahun}" : Penerbit;
    public string Inisial => string.IsNullOrWhiteSpace(Judul) ? "?" : Judul.Trim().Substring(0, 1).ToUpperInvariant();
}
