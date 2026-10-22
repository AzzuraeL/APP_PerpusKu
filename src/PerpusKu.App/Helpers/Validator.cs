using System.Net.Mail;

namespace PerpusKu.Helpers;

public static class Validator
{
    public static string Required(string value, string label, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return $"{label} wajib diisi.";
        if (maxLength > 0 && value.Trim().Length > maxLength) return $"{label} maksimal {maxLength} karakter.";
        return null;
    }

    public static string OptionalMax(string value, string label, int maxLength)
        => !string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength ? $"{label} maksimal {maxLength} karakter." : null;

    public static string Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null; // opsional
        try
        {
            var addr = new MailAddress(value.Trim());
            return addr.Address == value.Trim() ? null : "Format email tidak valid.";
        }
        catch { return "Format email tidak valid."; }
    }

    public static string IntRange(string value, string label, int min, int max, bool required)
    {
        if (string.IsNullOrWhiteSpace(value)) return required ? $"{label} wajib diisi." : null;
        if (!int.TryParse(value.Trim(), out var n)) return $"{label} harus berupa angka.";
        if (n < min || n > max) return $"{label} harus antara {min} dan {max}.";
        return null;
    }

    public static string Phone(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().All(c => char.IsDigit(c) || c == '+' || c == '-' || c == ' ')
            ? null : "Telepon hanya boleh berisi angka, +, - dan spasi.";
    }
}
