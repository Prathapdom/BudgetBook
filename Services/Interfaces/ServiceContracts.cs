using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Settings;

namespace BudgetBook.Services.Interfaces;

public interface ITransactionService
{
    Task<IReadOnlyList<TransactionRecord>> GetTransactionsAsync(DateTime? month = null, int? accountId = null, CancellationToken cancellationToken = default);
    Task SaveTransactionAsync(TransactionRecord transaction, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAccountService
{
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetAccountBalanceAsync(int accountId, CancellationToken cancellationToken = default);
    Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<MonthSummary> GetMonthSummaryAsync(DateTime month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySpend>> GetSpendByCategoryAsync(DateTime month, CancellationToken cancellationToken = default);
}

public interface ISettingsService
{
    string DatabasePath { get; }
    string DataFolder { get; }
    AppPreferences Preferences { get; }
    void SaveAppearance(AppThemeMode themeMode, AppAccentPalette accentPalette, AppDensity density);
    Task<string> ExportTransactionsCsvAsync(CancellationToken cancellationToken = default);
}
