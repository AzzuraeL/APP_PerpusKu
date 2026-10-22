using PerpusKu.Models;

namespace PerpusKu.Data.Repositories;

public class KategoriRepository
{
    public async Task<List<Kategori>> GetAllAsync()
    {
        var list = new List<Kategori>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT KategoriId, Nama, JumlahBuku FROM dbo.vw_BukuPerKategori ORDER BY Nama";
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new Kategori { KategoriId = r.Int("KategoriId"), Nama = r.Str("Nama"), JumlahBuku = r.Int("JumlahBuku") });
        return list;
    }
}
