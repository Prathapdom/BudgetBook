using System.Globalization;
using System.Text;
using System.Text.Json;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using BudgetBook.Settings;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace BudgetBook.Services;

public sealed class SettingsService : ISettingsService
{
    private sealed class DefaultContextFactory : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new();
    }

    private readonly ITransactionService transactionService;
    private readonly IDbContextFactory<AppDbContext> contextFactory;
    public SettingsService(ITransactionService transactionService) : this(transactionService, new DefaultContextFactory()) { }
    public SettingsService(ITransactionService transactionService, IDbContextFactory<AppDbContext> contextFactory)
    {
        this.transactionService = transactionService;
        this.contextFactory = contextFactory;
    }

    private sealed record BackupDocument(int Version, DateTime CreatedUtc, IReadOnlyList<Account> Accounts, IReadOnlyList<Category> Categories,
        IReadOnlyList<TransactionRecord> Transactions, IReadOnlyList<Budget>? Budgets = null,
        IReadOnlyList<RecurringTransaction>? RecurringTransactions = null, IReadOnlyList<ExchangeRateSnapshot>? ExchangeRates = null,
        IReadOnlyList<TransactionAttachment>? Attachments = null);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string DatabasePath => AppDbContext.DbPath;
    public string DataFolder => AppDbContext.DbFolder;
    private string PreferencesPath => Path.Combine(DataFolder, "settings.json");
    public AppPreferences Preferences { get; private set; } = LoadPreferences();

    public void SaveAppearance(AppThemeMode themeMode, AppAccentPalette accentPalette, AppDensity density)
    {
        Preferences = Preferences with { ThemeMode = themeMode, AccentPalette = accentPalette, Density = density };
        SavePreferences();
    }

    public void SaveRegional(string currencyCode, string localeName)
    {
        Preferences = Preferences with
        {
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant(),
            LocaleName = string.IsNullOrWhiteSpace(localeName) ? "en-US" : localeName.Trim(),
        };
        SavePreferences();
    }

    public async Task<string> ExportTransactionsCsvAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        var transactions = await transactionService.GetTransactionsAsync(cancellationToken: cancellationToken);
        var csv = new StringBuilder().AppendLine("Date,Type,Amount,Account,Category,Note,Tags");
        foreach (var transaction in transactions)
            csv.AppendLine(string.Join(',', transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), transaction.Type,
                transaction.Amount.ToString(CultureInfo.InvariantCulture), EscapeCsv(transaction.AccountName), EscapeCsv(transaction.CategoryName), EscapeCsv(transaction.Note), EscapeCsv(transaction.Tags)));
        await File.WriteAllTextAsync(destinationPath, csv.ToString(), cancellationToken);
        return destinationPath;
    }

    public async Task<string> ExportBackupJsonAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var backup = new BackupDocument(2, DateTime.UtcNow,
            await db.Accounts.AsNoTracking().ToListAsync(cancellationToken),
            await db.Categories.AsNoTracking().ToListAsync(cancellationToken),
            await db.Transactions.AsNoTracking().ToListAsync(cancellationToken),
            await db.Budgets.AsNoTracking().ToListAsync(cancellationToken),
            await db.RecurringTransactions.AsNoTracking().ToListAsync(cancellationToken),
            await db.ExchangeRates.AsNoTracking().ToListAsync(cancellationToken),
            await db.TransactionAttachments.AsNoTracking().ToListAsync(cancellationToken));
        await File.WriteAllTextAsync(destinationPath, JsonSerializer.Serialize(backup, JsonOptions), cancellationToken);
        return destinationPath;
    }

    public async Task RestoreBackupJsonAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var backup = JsonSerializer.Deserialize<BackupDocument>(json) ?? throw new InvalidDataException("The selected file is not a BudgetBook backup.");
        if (backup.Version is < 1 or > 2) throw new InvalidDataException($"Backup version {backup.Version} is not supported.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Budgets.ExecuteDeleteAsync(cancellationToken);
        await db.Transactions.ExecuteDeleteAsync(cancellationToken);
        await db.Categories.ExecuteDeleteAsync(cancellationToken);
        await db.Accounts.ExecuteDeleteAsync(cancellationToken);
        foreach (var account in backup.Accounts) { account.Transactions = []; db.Accounts.Add(account); }
        foreach (var category in backup.Categories) { category.Transactions = []; db.Categories.Add(category); }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var item in backup.Transactions) { item.Account = null; item.Category = null; db.Transactions.Add(item); }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var budget in backup.Budgets ?? []) { budget.Category = null; db.Budgets.Add(budget); }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var rule in backup.RecurringTransactions ?? []) { rule.Account = null; rule.Category = null; db.RecurringTransactions.Add(rule); }
        foreach (var rate in backup.ExchangeRates ?? [])
            if (!await db.ExchangeRates.AnyAsync(existing => existing.BaseCurrency == rate.BaseCurrency && existing.QuoteCurrency == rate.QuoteCurrency && existing.CapturedAt == rate.CapturedAt, cancellationToken)) db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<string> ExportBackupZipAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        var jsonPath = Path.Combine(Path.GetTempPath(), $"budgetbook-{Guid.NewGuid():N}.json");
        try
        {
            await ExportBackupJsonAsync(jsonPath, cancellationToken);
            using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(jsonPath, "budgetbook.json", CompressionLevel.Optimal);
            var backup = JsonSerializer.Deserialize<BackupDocument>(await File.ReadAllTextAsync(jsonPath, cancellationToken));
            foreach (var attachment in backup?.Attachments ?? [])
                if (File.Exists(attachment.StoredPath)) archive.CreateEntryFromFile(attachment.StoredPath, $"attachments/{attachment.Id}{Path.GetExtension(attachment.FileName)}", CompressionLevel.Optimal);
            return destinationPath;
        }
        finally { if (File.Exists(jsonPath)) File.Delete(jsonPath); }
    }

    public async Task RestoreBackupZipAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var extractFolder = Path.Combine(Path.GetTempPath(), $"budgetbook-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractFolder);
        try
        {
            ZipFile.ExtractToDirectory(sourcePath, extractFolder);
            var jsonPath = Path.Combine(extractFolder, "budgetbook.json");
            await RestoreBackupJsonAsync(jsonPath, cancellationToken);
            var backup = JsonSerializer.Deserialize<BackupDocument>(await File.ReadAllTextAsync(jsonPath, cancellationToken));
            var attachmentFolder = Path.Combine(DataFolder, "attachments"); Directory.CreateDirectory(attachmentFolder);
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            foreach (var attachment in backup?.Attachments ?? [])
            {
                var extracted = Path.Combine(extractFolder, "attachments", $"{attachment.Id}{Path.GetExtension(attachment.FileName)}");
                if (!File.Exists(extracted)) continue;
                attachment.StoredPath = Path.Combine(attachmentFolder, $"{attachment.TransactionRecordId}-{Guid.NewGuid():N}{Path.GetExtension(attachment.FileName)}");
                File.Copy(extracted, attachment.StoredPath); attachment.Transaction = null; db.TransactionAttachments.Add(attachment);
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (Directory.Exists(extractFolder)) Directory.Delete(extractFolder, true);
        }
    }

    private void SavePreferences() => File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(Preferences, JsonOptions));
    private static string EscapeCsv(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    private static AppPreferences LoadPreferences()
    {
        var path = Path.Combine(AppDbContext.DbFolder, "settings.json");
        if (!File.Exists(path)) return AppPreferences.Default;
        try
        {
            var loaded = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(path));
            return loaded is null ? AppPreferences.Default : loaded with
            {
                CurrencyCode = string.IsNullOrWhiteSpace(loaded.CurrencyCode) ? "USD" : loaded.CurrencyCode,
                LocaleName = string.IsNullOrWhiteSpace(loaded.LocaleName) ? "en-US" : loaded.LocaleName,
            };
        }
        catch (Exception exception) when (exception is JsonException or IOException) { return AppPreferences.Default; }
    }
}
