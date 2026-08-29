namespace BudgetBook.Settings;

public enum AppThemeMode
{
    System,
    Light,
    Dark,
}

public enum AppAccentPalette
{
    System,
    Ocean,
    Emerald,
    Violet,
    Sunset,
}

public enum AppDensity
{
    Comfortable,
    Compact,
}

public sealed record AppPreferences(
    AppThemeMode ThemeMode,
    AppAccentPalette AccentPalette,
    AppDensity Density,
    string CurrencyCode = "USD",
    string LocaleName = "en-US")
{
    public static AppPreferences Default { get; } = new(
        AppThemeMode.System,
        AppAccentPalette.System,
        AppDensity.Comfortable,
        "USD",
        "en-US");
}
