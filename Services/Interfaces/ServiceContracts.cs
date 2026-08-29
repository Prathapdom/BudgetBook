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

public interface IBudgetService
{
    Task<IReadOnlyList<Budget>> GetBudgetsAsync(CancellationToken cancellationToken = default);
    Task<BudgetVsActualReport> GetBudgetReportAsync(ReportFilter filter, DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default);
    Task DeleteBudgetAsync(int id, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<DashboardSummary> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryBreakdown>> GetExpenseBreakdownAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashFlowPoint>> GetCashFlowAsync(ReportFilter filter, ReportGranularity granularity = ReportGranularity.Auto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IncomeExpensePoint>> GetIncomeExpenseAsync(ReportFilter filter, ReportGranularity granularity = ReportGranularity.Auto, CancellationToken cancellationToken = default);
    Task<PeriodComparison> GetPeriodComparisonAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountBalanceSummary>> GetAccountBalanceDistributionAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentTransaction>> GetRecentTransactionsAsync(ReportFilter filter, int count = 10, CancellationToken cancellationToken = default);

    // Compatibility methods retained while the Phase 3 dashboard moves to the new filter API.
    Task<MonthSummary> GetMonthSummaryAsync(DateTime month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySpend>> GetSpendByCategoryAsync(DateTime month, CancellationToken cancellationToken = default);
}

public interface ISettingsService
{
    string DatabasePath { get; }
    string DataFolder { get; }
    AppPreferences Preferences { get; }
    void SaveAppearance(AppThemeMode themeMode, AppAccentPalette accentPalette, AppDensity density);
    void SaveRegional(string currencyCode, string localeName);
    Task<string> ExportTransactionsCsvAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task<string> ExportBackupJsonAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task RestoreBackupJsonAsync(string sourcePath, CancellationToken cancellationToken = default);
}
