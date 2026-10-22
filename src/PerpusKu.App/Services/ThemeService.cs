using System.Text.Json;
using System.Windows;

namespace PerpusKu.Services;

public static class ThemeService
{
    private static readonly string PrefsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PerpusKu", "prefs.json");

    public static bool IsDark { get; private set; }
    public static event Action Changed;

    public static void LoadSaved()
    {
        try
        {
            if (File.Exists(PrefsPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(PrefsPath));
                if (doc.RootElement.TryGetProperty("dark", out var d)) IsDark = d.GetBoolean();
            }
        }
        catch { /* pakai tema terang */ }
        Apply(IsDark, save: false);
    }

    public static void Apply(bool dark, bool save = true)
    {
        IsDark = dark;
        var uri = new Uri($"/PerpusKu;component/Themes/{(dark ? "Dark" : "Light")}Theme.xaml", UriKind.Relative);
        var merged = Application.Current.Resources.MergedDictionaries;
        merged[0] = new ResourceDictionary { Source = uri };
        if (save) Save();
        Changed?.Invoke();
    }

    public static void Toggle() => Apply(!IsDark);

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath)!);
            File.WriteAllText(PrefsPath, JsonSerializer.Serialize(new { dark = IsDark }));
        }
        catch { /* preferensi tidak kritis */ }
    }
}
