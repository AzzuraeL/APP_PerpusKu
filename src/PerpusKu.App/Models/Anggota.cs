namespace PerpusKu.Models;

public class Anggota
{
    public int AnggotaId { get; set; }
    public string NomorAnggota { get; set; }
    public string Nama { get; set; }
    public string Tipe { get; set; }
    public string Email { get; set; }
    public string Telepon { get; set; }
    public DateTime TanggalDaftar { get; set; }
    public int SedangPinjam { get; set; }
}
