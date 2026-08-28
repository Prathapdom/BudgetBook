using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BudgetBook.Services.Interfaces;
using BudgetBook.Settings;

namespace BudgetBook.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly ISettingsService _settings;
    private readonly IThemeManager _themeManager;
    public string DbPath => _settings.DatabasePath;

    private bool _loading = true;

    public SettingsPage()
    {
        _settings = App.GetService<ISettingsService>();
        _themeManager = App.GetService<IThemeManager>();
        InitializeComponent();

        ThemeBox.SelectedIndex = _settings.Preferences.ThemeMode switch
        {
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0,
        };
        AccentBox.SelectedIndex = (int)_settings.Preferences.AccentPalette;
        DensityBox.SelectedIndex = (int)_settings.Preferences.Density;
        _loading = false;
    }

    private void Appearance_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || App.RootElement is null) return;

        var themeMode = ThemeBox.SelectedIndex switch
        {
            1 => AppThemeMode.Light,
            2 => AppThemeMode.Dark,
            _ => AppThemeMode.System,
        };
        var accentPalette = AccentBox.SelectedIndex >= 0
            ? (AppAccentPalette)AccentBox.SelectedIndex
            : AppAccentPalette.System;
        var density = DensityBox.SelectedIndex == 1 ? AppDensity.Compact : AppDensity.Comfortable;

        _settings.SaveAppearance(themeMode, accentPalette, density);
        _themeManager.Apply(App.RootElement, _settings.Preferences);
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var path = await _settings.ExportTransactionsCsvAsync();

        await new ContentDialog
        {
            Title = "Export complete",
            Content = $"Saved to:\n{path}",
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        }.ShowAsync();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
        => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _settings.DataFolder,
            UseShellExecute = true,
        });
}
