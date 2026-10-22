namespace PerpusKu.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string NamaLengkap { get; set; }
    public string Role { get; set; }
    public string Inisial => string.IsNullOrWhiteSpace(NamaLengkap) ? "?" : NamaLengkap.Trim().Substring(0, 1).ToUpperInvariant();
}

public class UserCredential
{
    public User User { get; set; }
    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
}
