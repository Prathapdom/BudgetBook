using System.Globalization;
using System.Text;
using System.Text.Json;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using BudgetBook.Settings;

namespace BudgetBook.Services;

public sealed class SettingsService(ITransactionService transactionService) : ISettingsService
{
    public string DatabasePath => AppDbContext.DbPath;
    public string DataFolder => AppDbContext.DbFolder;
    private string PreferencesPath => Path.Combine(DataFolder, "settings.json");
    public AppPreferences Preferences { get; private set; } = LoadAppearance();

    public void SaveAppearance(AppThemeMode themeMode, AppAccentPalette accentPalette, AppDensity density)
    {
        Preferences = new AppPreferences(themeMode, accentPalette, density);
        File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(Preferences, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
    }

    public async Task<string> ExportTransactionsCsvAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await transactionService.GetTransactionsAsync(cancellationToken: cancellationToken);
        var csv = new StringBuilder().AppendLine("Date,Type,Amount,Account,Category,Note");
        foreach (var transaction in transactions)
        {
            csv.AppendLine(string.Join(',', transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), transaction.Type,
                transaction.Amount.ToString(CultureInfo.InvariantCulture), EscapeCsv(transaction.AccountName),
                EscapeCsv(transaction.CategoryName), EscapeCsv(transaction.Note)));
        }

        var path = Path.Combine(DataFolder, $"budgetbook-export-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        await File.WriteAllTextAsync(path, csv.ToString(), cancellationToken);
        return path;
    }

    private static string EscapeCsv(string value)
        => value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    private static AppPreferences LoadAppearance()
    {
        var path = Path.Combine(AppDbContext.DbFolder, "settings.json");
        if (!File.Exists(path)) return AppPreferences.Default;

        try
        {
            return JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(path))
                ?? AppPreferences.Default;
        }
        catch (JsonException)
        {
            return AppPreferences.Default;
        }
        catch (IOException)
        {
            return AppPreferences.Default;
        }
    }
}
