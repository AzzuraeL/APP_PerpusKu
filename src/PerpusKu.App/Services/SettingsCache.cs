namespace PerpusKu.Services;

/// <summary>Cache nilai aturan perpustakaan untuk tampilan (perhitungan denda sebenarnya tetap di database).</summary>
public static class SettingsCache
{
    public static decimal DendaPerHari { get; set; } = 1000;
    public static int LamaPinjamHari { get; set; } = 7;
}
