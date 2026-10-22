namespace PerpusKu.Models;

public class Kategori
{
    public int KategoriId { get; set; }
    public string Nama { get; set; }
    public int JumlahBuku { get; set; }
    public override string ToString() => Nama;
}
