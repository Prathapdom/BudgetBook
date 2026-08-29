using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using BudgetBook.Services.Interfaces;
using BudgetBook.Settings;
using BudgetBook.Data;

namespace BudgetBook.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly ISettingsService _settings;
    private readonly IThemeManager _themeManager;
    private readonly ICsvImportService _csvImport;
    private readonly IExchangeRateService _rates;
    private readonly IPdfReportService _pdfReports;
    public string DbPath => _settings.DatabasePath;
    private bool _loading = true;

    public SettingsPage()
    {
        _settings = App.GetService<ISettingsService>();
        _themeManager = App.GetService<IThemeManager>();
        _csvImport = App.GetService<ICsvImportService>();
        _rates = App.GetService<IExchangeRateService>();
        _pdfReports = App.GetService<IPdfReportService>();
        InitializeComponent();
        ThemeBox.SelectedIndex = _settings.Preferences.ThemeMode switch { AppThemeMode.Light => 1, AppThemeMode.Dark => 2, _ => 0 };
        AccentBox.SelectedIndex = (int)_settings.Preferences.AccentPalette;
        DensityBox.SelectedIndex = (int)_settings.Preferences.Density;
        SelectContent(CurrencyBox, _settings.Preferences.CurrencyCode);
        SelectContent(LocaleBox, _settings.Preferences.LocaleName);
        _loading = false;
    }

    private static void SelectContent(ComboBox box, string value)
    {
        for (var index = 0; index < box.Items.Count; index++)
            if (box.Items[index] is ComboBoxItem item && string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase)) { box.SelectedIndex = index; return; }
        box.SelectedIndex = 0;
    }

    private void Appearance_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || App.RootElement is null) return;
        var theme = ThemeBox.SelectedIndex switch { 1 => AppThemeMode.Light, 2 => AppThemeMode.Dark, _ => AppThemeMode.System };
        var accent = AccentBox.SelectedIndex >= 0 ? (AppAccentPalette)AccentBox.SelectedIndex : AppAccentPalette.System;
        var density = DensityBox.SelectedIndex == 1 ? AppDensity.Compact : AppDensity.Comfortable;
        _settings.SaveAppearance(theme, accent, density);
        _themeManager.Apply(App.RootElement, _settings.Preferences);
    }

    private void Regional_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _settings.SaveRegional(SelectedContent(CurrencyBox, "USD"), SelectedContent(LocaleBox, "en-US"));
        ShowStatus("Regional preferences saved.");
    }

    private static string SelectedContent(ComboBox box, string fallback) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? fallback;

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedFileName = $"budgetbook-transactions-{DateTime.Today:yyyyMMdd}" };
        picker.FileTypeChoices.Add("CSV file", [".csv"]); InitializePicker(picker);
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        await _settings.ExportTransactionsCsvAsync(file.Path);
        ShowStatus($"CSV exported to {file.Path}");
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedFileName = $"budgetbook-backup-{DateTime.Today:yyyyMMdd}" };
        picker.FileTypeChoices.Add("BudgetBook portable backup", [".zip"]); InitializePicker(picker);
        var file = await picker.PickSaveFileAsync();
        if (file is null) return;
        await _settings.ExportBackupZipAsync(file.Path);
        ShowStatus($"Backup exported to {file.Path}");
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedFileName = $"budgetbook-data-{DateTime.Today:yyyyMMdd}" };
        picker.FileTypeChoices.Add("BudgetBook JSON", [".json"]); InitializePicker(picker);
        var file = await picker.PickSaveFileAsync(); if (file is null) return;
        await _settings.ExportBackupJsonAsync(file.Path); ShowStatus($"JSON exported to {file.Path}");
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".json"); picker.FileTypeFilter.Add(".zip"); InitializePicker(picker);
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        var result = await new ContentDialog { Title = "Replace all BudgetBook data?", Content = "Restore replaces every current account, category, and transaction. Export a backup first if you may need the current data.", PrimaryButtonText = "Restore", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close, XamlRoot = XamlRoot }.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        try { if (file.FileType.Equals(".zip", StringComparison.OrdinalIgnoreCase)) await _settings.RestoreBackupZipAsync(file.Path); else await _settings.RestoreBackupJsonAsync(file.Path); ShowStatus("Backup restored. Reopen a page to refresh its data."); }
        catch (Exception exception) { StatusBar.Severity = InfoBarSeverity.Error; StatusBar.Title = "Restore failed"; StatusBar.Message = exception.Message; StatusBar.IsOpen = true; }
    }

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(); picker.FileTypeFilter.Add(".csv"); InitializePicker(picker);
        var file = await picker.PickSingleFileAsync(); if (file is null) return;
        var preview = await _csvImport.PreviewAsync(file.Path);
        var message = $"Ready: {preview.Transactions.Count}\nDuplicates skipped: {preview.DuplicateCount}\nErrors: {preview.Errors.Count}";
        if (preview.Errors.Count > 0) message += "\n\n" + string.Join("\n", preview.Errors.Take(5));
        var result = await new ContentDialog { Title = "CSV import preview", Content = message, PrimaryButtonText = "Import", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary, XamlRoot = XamlRoot }.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        var count = await _csvImport.ImportAsync(preview); ShowStatus($"Imported {count} transactions.");
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedFileName = $"budgetbook-report-{DateTime.Today:yyyy-MM}" }; picker.FileTypeChoices.Add("PDF report", [".pdf"]); InitializePicker(picker);
        var file = await picker.PickSaveFileAsync(); if (file is null) return;
        await _pdfReports.ExportMonthlyReportAsync(file.Path, DateTime.Today); ShowStatus($"PDF report exported to {file.Path}");
    }

    private async void Rates_Click(object sender, RoutedEventArgs e)
    {
        var from = new TextBox { Header = "From currency", Text = _settings.Preferences.CurrencyCode, MaxLength = 3 };
        var to = new TextBox { Header = "To currency", Text = "USD", MaxLength = 3 };
        var value = new NumberBox { Header = "Rate", Minimum = 0.000001, Value = 1, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        var date = new DatePicker { Header = "Snapshot date", Date = DateTimeOffset.Now };
        var panel = new StackPanel { Spacing = 10, MinWidth = 320 }; panel.Children.Add(from); panel.Children.Add(to); panel.Children.Add(value); panel.Children.Add(date);
        var dialog = new ContentDialog { Title = "Add exchange-rate snapshot", PrimaryButtonText = "Save", CloseButtonText = "Cancel", Content = panel, XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { await _rates.SaveRateAsync(new ExchangeRateSnapshot { BaseCurrency = from.Text, QuoteCurrency = to.Text, Rate = (decimal)value.Value, CapturedAt = date.Date.DateTime }); ShowStatus("Exchange-rate snapshot saved."); }
        catch (Exception exception) { StatusBar.Severity = InfoBarSeverity.Error; StatusBar.Title = "Rate not saved"; StatusBar.Message = exception.Message; StatusBar.IsOpen = true; }
    }

    private static void InitializePicker(object picker)
    {
        if (MainWindow.Instance is null) return;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);
    }

    private void ShowStatus(string message) { StatusBar.Severity = InfoBarSeverity.Success; StatusBar.Title = message; StatusBar.Message = string.Empty; StatusBar.IsOpen = true; }
    private void OpenFolder_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _settings.DataFolder, UseShellExecute = true });
}
