using PerpusKu.Data.Repositories;
using PerpusKu.Helpers;

namespace PerpusKu.Services;

public class AuthService
{
    private readonly UserRepository _users = new();

    public async Task<(bool Ok, string Error)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return (false, "Username dan password wajib diisi.");

        var cred = await _users.GetByUsernameAsync(username);
        // pesan sengaja sama agar tidak membocorkan apakah username ada
        if (cred == null || !PasswordHasher.Verify(password, cred.PasswordHash, cred.PasswordSalt))
            return (false, "Username atau password salah.");

        SessionService.SignIn(cred.User);
        await PengaturanRepository.LoadCacheAsync();
        return (true, null);
    }

    public async Task<string> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        if (!SessionService.IsLoggedIn) return "Sesi tidak valid.";
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8) return "Password baru minimal 8 karakter.";
        if (!newPassword.Any(char.IsDigit) || !newPassword.Any(char.IsLetter)) return "Password baru harus mengandung huruf dan angka.";

        var cred = await _users.GetByIdAsync(SessionService.Current.UserId);
        if (cred == null || !PasswordHasher.Verify(oldPassword ?? "", cred.PasswordHash, cred.PasswordSalt))
            return "Password lama tidak sesuai.";

        var (hash, salt) = PasswordHasher.Hash(newPassword);
        await _users.UpdatePasswordAsync(cred.User.UserId, hash, salt);
        return null;
    }
}
