using System.Globalization;
using PerpusKu.Data.Repositories;
using PerpusKu.Models;

namespace PerpusKu.Services;

/// <summary>Lapisan tipis di atas repository untuk alur sirkulasi (aturan utamanya ada di stored procedure).</summary>
public class SirkulasiService
{
    private readonly PeminjamanRepository _repo = new();
    private readonly BukuRepository _buku = new();
    private readonly AnggotaRepository _anggota = new();

    public Task<Buku> CariBukuAsync(string kode) => _buku.GetByKodeAsync(kode);
    public Task<Anggota> CariAnggotaAsync(string nomor) => _anggota.GetByNomorAsync(nomor);
    public Task<List<Peminjaman>> CariAktifAsync(string keyword) => _repo.GetAktifAsync(keyword);

    public async Task<string> PinjamAsync(string kode, string nomor)
    {
        var tempo = await _repo.PinjamAsync(kode, nomor);
        return $"Peminjaman berhasil. Jatuh tempo {tempo:dd MMMM yyyy}.";
    }

    public async Task<string> KembalikanAsync(int id)
    {
        var (denda, hari) = await _repo.KembalikanAsync(id);
        return denda > 0
            ? $"Buku dikembalikan. Terlambat {hari} hari, denda {Rupiah(denda)}."
            : "Buku berhasil dikembalikan tanpa denda.";
    }

    public async Task<string> PerpanjangAsync(int id)
    {
        var tempo = await _repo.PerpanjangAsync(id);
        return $"Masa pinjam diperpanjang hingga {tempo:dd MMMM yyyy}.";
    }

    public static string Rupiah(decimal value) => string.Format(new CultureInfo("id-ID"), "Rp {0:N0}", value);
}
