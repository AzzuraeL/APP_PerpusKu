using System.Data;
using PerpusKu.Models;
using PerpusKu.Services;

namespace PerpusKu.Data.Repositories;

public class PengaturanRepository
{
    public async Task<List<PengaturanItem>> GetAllAsync()
    {
        var list = new List<PengaturanItem>();
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Kunci, Nilai, Label FROM dbo.Pengaturan ORDER BY Kunci";
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new PengaturanItem { Kunci = r.Str("Kunci"), Nilai = r.Str("Nilai"), Label = r.Str("Label") });
        return list;
    }

    public async Task UpdateAsync(string kunci, string nilai)
    {
        SessionService.Require(Permission.KelolaPengaturan);
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.Pengaturan SET Nilai = @n WHERE Kunci = @k";
        cmd.Add("@n", SqlDbType.NVarChar, nilai);
        cmd.Add("@k", SqlDbType.NVarChar, kunci);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task LoadCacheAsync()
    {
        try
        {
            var items = await new PengaturanRepository().GetAllAsync();
            foreach (var i in items)
            {
                if (i.Kunci == "DendaPerHari" && decimal.TryParse(i.Nilai, out var d)) SettingsCache.DendaPerHari = d;
                if (i.Kunci == "LamaPinjamHari" && int.TryParse(i.Nilai, out var l)) SettingsCache.LamaPinjamHari = l;
            }
        }
        catch { /* gunakan nilai bawaan */ }
    }
}
