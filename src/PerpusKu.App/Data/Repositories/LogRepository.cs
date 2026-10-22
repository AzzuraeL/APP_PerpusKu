using System.Data;
using PerpusKu.Services;

namespace PerpusKu.Data.Repositories;

public class LogRepository
{
    public static async Task AddAsync(string aksi, string entitas, string deskripsi)
    {
        try
        {
            await using var conn = await Db.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT dbo.LogAktivitas (UserId, JenisAksi, Entitas, Deskripsi) VALUES (@u, @a, @e, @d)";
            cmd.Add("@u", SqlDbType.Int, SessionService.CurrentUserId);
            cmd.Add("@a", SqlDbType.NVarChar, aksi);
            cmd.Add("@e", SqlDbType.NVarChar, entitas);
            cmd.Add("@d", SqlDbType.NVarChar, deskripsi.Length > 250 ? deskripsi[..250] : deskripsi);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // pencatatan log tidak boleh menggagalkan aksi utama
        }
    }
}
