using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace PerpusKu.Data;

public static class Db
{
    private static string _connectionString;

    public static string ConnectionString
    {
        get
        {
            if (_connectionString != null) return _connectionString;
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("File appsettings.json tidak ditemukan di folder aplikasi.", path);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            _connectionString = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("PerpusKu").GetString();
            return _connectionString;
        }
    }

    public static async Task<SqlConnection> OpenAsync()
    {
        var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }
}
