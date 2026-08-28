using System;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BudgetBook.Data;

namespace BudgetBook.Pages;

public sealed partial class SettingsPage : Page
{
    public string DbPath => AppDbContext.DbPath;

    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();

        ThemeBox.SelectedIndex = (App.RootElement?.RequestedTheme ?? ElementTheme.Default) switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0,
        };
        _loading = false;
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || App.RootElement is null) return;

        App.RootElement.RequestedTheme = ThemeBox.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Type,Amount,Account,Category,Note");
        foreach (var t in App.Budget.GetTransactions())
        {
            sb.AppendLine(string.Join(',',
                t.Date.ToString("yyyy-MM-dd"),
                t.Type,
                t.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Csv(t.AccountName),
                Csv(t.CategoryName),
                Csv(t.Note)));
        }

        var path = Path.Combine(AppDbContext.DbFolder,
            $"budgetbook-export-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        File.WriteAllText(path, sb.ToString());

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
            FileName = AppDbContext.DbFolder,
            UseShellExecute = true,
        });

    private static string Csv(string s)
        => s.Contains(',') || s.Contains('"')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
}
