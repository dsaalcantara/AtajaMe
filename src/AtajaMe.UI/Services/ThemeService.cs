using Microsoft.Win32;

namespace AtajaMe.UI.Services;

public interface IThemeService
{
    string CurrentTheme { get; }
    bool IsDarkActive { get; }
    void ApplyTheme(string themePreference);
}

/// <summary>
/// Gestiona la detección e intercambio dinámico de temas visuales (Claro / Oscuro) según RF-38.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string ThemeRegistrySubKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    private string _currentThemePreference = "System";
    private bool _isDarkActive;

    public string CurrentTheme => _currentThemePreference;
    public bool IsDarkActive => _isDarkActive;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && _currentThemePreference == "System")
        {
            ApplyTheme("System");
        }
    }

    public void ApplyTheme(string themePreference)
    {
        _currentThemePreference = themePreference switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            _ => "System"
        };

        bool useDark = _currentThemePreference switch
        {
            "Dark" => true,
            "Light" => false,
            _ => DetectWindowsDarkTheme()
        };

        _isDarkActive = useDark;
        SwitchResources(useDark);
    }

    private static bool DetectWindowsDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemeRegistrySubKey);
            var value = key?.GetValue(AppsUseLightThemeValue);
            if (value is int intVal)
            {
                return intVal == 0; // 0 significa Dark Mode en Windows
            }
        }
        catch
        {
            // Ignorar errores de lectura de registro y fallback a Light
        }

        return false;
    }

    private static void SwitchResources(bool isDark)
    {
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            return;
        }

        var themeUri = isDark
            ? new Uri("pack://application:,,,/AtajaMe.UI;component/Themes/DarkTheme.xaml", UriKind.Absolute)
            : new Uri("pack://application:,,,/AtajaMe.UI;component/Themes/LightTheme.xaml", UriKind.Absolute);

        var existingThemeDict = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));

        if (existingThemeDict != null)
        {
            app.Resources.MergedDictionaries.Remove(existingThemeDict);
        }

        app.Resources.MergedDictionaries.Insert(0, new System.Windows.ResourceDictionary { Source = themeUri });

        // Actualizar barra de título nativa DWM en todas las ventanas activas
        foreach (System.Windows.Window window in app.Windows)
        {
            Common.WindowThemeHelper.ApplyDarkTitleBar(window, isDark);
        }
    }
}
