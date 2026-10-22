using System.Data;
using PerpusKu.Models;

namespace PerpusKu.Data.Repositories;

public class UserRepository
{
    public async Task<UserCredential> GetByUsernameAsync(string username)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT UserId, Username, NamaLengkap, Role, PasswordHash, PasswordSalt
                            FROM dbo.Users WHERE Username = @u AND IsActive = 1";
        cmd.Add("@u", SqlDbType.NVarChar, username.Trim());
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new UserCredential
        {
            User = new User { UserId = r.Int("UserId"), Username = r.Str("Username"), NamaLengkap = r.Str("NamaLengkap"), Role = r.Str("Role") },
            PasswordHash = r.Str("PasswordHash"),
            PasswordSalt = r.Str("PasswordSalt")
        };
    }

    public async Task<UserCredential> GetByIdAsync(int userId)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT UserId, Username, NamaLengkap, Role, PasswordHash, PasswordSalt
                            FROM dbo.Users WHERE UserId = @id AND IsActive = 1";
        cmd.Add("@id", SqlDbType.Int, userId);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new UserCredential
        {
            User = new User { UserId = r.Int("UserId"), Username = r.Str("Username"), NamaLengkap = r.Str("NamaLengkap"), Role = r.Str("Role") },
            PasswordHash = r.Str("PasswordHash"),
            PasswordSalt = r.Str("PasswordSalt")
        };
    }

    public async Task UpdatePasswordAsync(int userId, string hash, string salt)
    {
        await using var conn = await Db.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.Users SET PasswordHash = @h, PasswordSalt = @s WHERE UserId = @id";
        cmd.Add("@h", SqlDbType.NVarChar, hash);
        cmd.Add("@s", SqlDbType.NVarChar, salt);
        cmd.Add("@id", SqlDbType.Int, userId);
        await cmd.ExecuteNonQueryAsync();
    }
}
