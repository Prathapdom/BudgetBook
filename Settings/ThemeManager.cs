using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace BudgetBook.Settings;

public interface IThemeManager
{
    void Apply(FrameworkElement root, AppPreferences preferences);
}

public sealed class ThemeManager : IThemeManager
{
    public void Apply(FrameworkElement root, AppPreferences preferences)
    {
        root.RequestedTheme = preferences.ThemeMode switch
        {
            AppThemeMode.Light => ElementTheme.Light,
            AppThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        ApplyAccent(preferences.AccentPalette);
        ApplyDensity(preferences.Density);
        root.InvalidateMeasure();
        root.UpdateLayout();
    }

    private static void ApplyAccent(AppAccentPalette palette)
    {
        var color = palette switch
        {
            AppAccentPalette.Ocean => Color.FromArgb(255, 0, 103, 192),
            AppAccentPalette.Emerald => Color.FromArgb(255, 14, 122, 95),
            AppAccentPalette.Violet => Color.FromArgb(255, 116, 77, 169),
            AppAccentPalette.Sunset => Color.FromArgb(255, 201, 79, 54),
            _ => new UISettings().GetColorValue(UIColorType.Accent),
        };

        var subtle = Color.FromArgb(54, color.R, color.G, color.B);
        var foreground = RelativeLuminance(color) > 0.48
            ? Color.FromArgb(255, 0, 0, 0)
            : Color.FromArgb(255, 255, 255, 255);

        foreach (var (name, dictionary) in EnumerateThemeDictionaries(Application.Current.Resources))
        {
            if (string.Equals(name, "HighContrast", StringComparison.OrdinalIgnoreCase))
                continue;

            SetBrushColor(dictionary, "AppAccentBrush", color);
            SetBrushColor(dictionary, "AppAccentSubtleBrush", subtle);
            SetBrushColor(dictionary, "AppAccentForegroundBrush", foreground);
        }
    }

    private static void ApplyDensity(AppDensity density)
    {
        var pagePadding = density == AppDensity.Compact
            ? new Thickness(16, 12, 16, 12)
            : new Thickness(24, 16, 24, 16);
        var cardPadding = density == AppDensity.Compact ? new Thickness(12) : new Thickness(16);
        var sectionSpacing = density == AppDensity.Compact ? 12d : 16d;
        var controlSpacing = density == AppDensity.Compact ? 6d : 8d;

        SetResource(Application.Current.Resources, "AppPagePadding", pagePadding);
        SetResource(Application.Current.Resources, "AppCardPadding", cardPadding);
        SetResource(Application.Current.Resources, "AppSectionSpacing", sectionSpacing);
        SetResource(Application.Current.Resources, "AppControlSpacing", controlSpacing);
    }

    private static IEnumerable<(string Name, ResourceDictionary Dictionary)> EnumerateThemeDictionaries(
        ResourceDictionary dictionary)
    {
        foreach (var pair in dictionary.ThemeDictionaries)
        {
            if (pair.Value is ResourceDictionary themeDictionary)
                yield return (pair.Key?.ToString() ?? string.Empty, themeDictionary);
        }

        foreach (var mergedDictionary in dictionary.MergedDictionaries)
        {
            foreach (var item in EnumerateThemeDictionaries(mergedDictionary))
                yield return item;
        }
    }

    private static bool SetResource(ResourceDictionary dictionary, string key, object value)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary[key] = value;
            return true;
        }

        foreach (var mergedDictionary in dictionary.MergedDictionaries)
        {
            if (SetResource(mergedDictionary, key, value)) return true;
        }

        return false;
    }

    private static void SetBrushColor(ResourceDictionary dictionary, string key, Color color)
    {
        if (dictionary.ContainsKey(key) && dictionary[key] is SolidColorBrush brush)
            brush.Color = color;
    }

    private static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var normalized = value / 255d;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }
}
