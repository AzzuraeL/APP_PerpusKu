using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PerpusKu.Data;

/// <summary>
/// Membuat database + tabel + data awal secara otomatis jika belum ada,
/// sehingga aplikasi bisa langsung jalan tanpa menjalankan skrip secara manual.
/// </summary>
public static class DatabaseInitializer
{
    private static readonly Regex GoSplit = new(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task EnsureAsync()
    {
        var target = new SqlConnectionStringBuilder(Db.ConnectionString);
        var dbName = target.InitialCatalog;

        var master = new SqlConnectionStringBuilder(Db.ConnectionString) { InitialCatalog = "master" };
        bool dbExists;
        await using (var c = new SqlConnection(master.ConnectionString))
        {
            await c.OpenAsync();
            await using var cmd = new SqlCommand("SELECT DB_ID(@n)", c);
            cmd.Parameters.AddWithValue("@n", dbName);
            var o = await cmd.ExecuteScalarAsync();
            dbExists = o != null && o != DBNull.Value;

            if (!dbExists)
                await RunScriptAsync(c, "01_CreateDatabase.sql", dbName);
        }

        bool complete = false;
        if (dbExists)
        {
            await using var c = new SqlConnection(target.ConnectionString);
            await c.OpenAsync();
            await using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.vw_PeminjamanDetail') IS NOT NULL AND OBJECT_ID(N'dbo.sp_PinjamBuku') IS NOT NULL THEN 1 ELSE 0 END", c);
            complete = Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
        }

        if (complete) return;

        await using var conn = new SqlConnection(target.ConnectionString);
        await conn.OpenAsync();
        foreach (var file in new[] { "02_CreateTables.sql", "03_StoredProcedures.sql", "04_SeedData.sql", "05_Views.sql" })
            await RunScriptAsync(conn, file, dbName);
    }

    private static async Task RunScriptAsync(SqlConnection conn, string fileName, string dbName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Database", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Skrip database {fileName} tidak ditemukan di folder Database.", path);

        var sql = File.ReadAllText(path).Replace("PerpusKu_DB", dbName);
        foreach (var batch in GoSplit.Split(sql))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var cmd = new SqlCommand(batch, conn) { CommandTimeout = 60 };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
