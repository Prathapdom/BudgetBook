using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class ReportService(IDbContextFactory<AppDbContext> contextFactory) : IReportService
{
    public async Task<DashboardSummary> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await LoadTransactionsAsync(db, validated, cancellationToken);
        return FinanceCalculator.Summarize(validated, transactions);
    }

    public async Task<IReadOnlyList<CategoryBreakdown>> GetExpenseBreakdownAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await LoadTransactionsAsync(db, validated, cancellationToken, TransactionType.Expense);
        return FinanceCalculator.ExpenseBreakdown(transactions);
    }

    public async Task<IReadOnlyList<CashFlowPoint>> GetCashFlowAsync(
        ReportFilter filter,
        ReportGranularity granularity = ReportGranularity.Auto,
        CancellationToken cancellationToken = default)
    {
        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await LoadTransactionsAsync(db, validated, cancellationToken);
        return FinanceCalculator.CashFlowSeries(validated, transactions, granularity);
    }

    public async Task<IReadOnlyList<IncomeExpensePoint>> GetIncomeExpenseAsync(
        ReportFilter filter,
        ReportGranularity granularity = ReportGranularity.Auto,
        CancellationToken cancellationToken = default)
    {
        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await LoadTransactionsAsync(db, validated, cancellationToken);
        return FinanceCalculator.IncomeExpenseSeries(validated, transactions, granularity);
    }

    public async Task<PeriodComparison> GetPeriodComparisonAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var currentFilter = filter.Validate();
        var previousFilter = new ReportFilter(
            currentFilter.StartDate.AddDays(-currentFilter.DayCount),
            currentFilter.StartDate,
            currentFilter.AccountId,
            currentFilter.CategoryId);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var combinedFilter = new ReportFilter(previousFilter.StartDate, currentFilter.EndDateExclusive,
            currentFilter.AccountId, currentFilter.CategoryId);
        var transactions = await LoadTransactionsAsync(db, combinedFilter, cancellationToken);

        var current = FinanceCalculator.Summarize(currentFilter,
            transactions.Where(t => t.Date >= currentFilter.StartDate && t.Date < currentFilter.EndDateExclusive));
        var previous = FinanceCalculator.Summarize(previousFilter,
            transactions.Where(t => t.Date >= previousFilter.StartDate && t.Date < previousFilter.EndDateExclusive));
        return new PeriodComparison(current, previous);
    }

    public async Task<IReadOnlyList<AccountBalanceSummary>> GetAccountBalanceDistributionAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var accountQuery = db.Accounts.AsNoTracking().AsQueryable();
        if (validated.AccountId is { } accountId)
            accountQuery = accountQuery.Where(account => account.Id == accountId);

        var accounts = await accountQuery.OrderBy(account => account.Name).ToListAsync(cancellationToken);

        // A balance is an as-of value, so it includes movement before the selected period.
        var transactionQuery = db.Transactions.AsNoTracking()
            .Where(transaction => transaction.Date < validated.EndDateExclusive);
        if (validated.AccountId is { } selectedAccountId)
            transactionQuery = transactionQuery.Where(transaction => transaction.AccountId == selectedAccountId);
        if (validated.CategoryId is { } categoryId)
            transactionQuery = transactionQuery.Where(transaction => transaction.CategoryId == categoryId);

        var movements = await transactionQuery
            .Select(transaction => new { transaction.AccountId, transaction.Type, transaction.Amount })
            .ToListAsync(cancellationToken);

        var balances = accounts.Select(account => new
        {
            Account = account,
            Balance = account.OpeningBalance + movements
                .Where(transaction => transaction.AccountId == account.Id)
                .Sum(transaction => transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount),
        }).ToList();

        var positiveTotals = balances
            .GroupBy(row => row.Account.Currency)
            .ToDictionary(group => group.Key, group => group.Sum(row => Math.Max(0m, row.Balance)));

        return balances.Select(row => new AccountBalanceSummary(
                row.Account.Id,
                row.Account.Name,
                row.Account.Currency,
                row.Account.OpeningBalance,
                row.Balance,
                positiveTotals[row.Account.Currency] == 0m
                    ? 0m
                    : Math.Max(0m, row.Balance) / positiveTotals[row.Account.Currency]))
            .OrderByDescending(row => row.Balance)
            .ThenBy(row => row.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<RecentTransaction>> GetRecentTransactionsAsync(
        ReportFilter filter,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0) return [];

        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = ApplyFilter(db.Transactions.AsNoTracking(), validated);

        return await query
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .Take(count)
            .Select(transaction => new RecentTransaction(
                transaction.Id,
                transaction.Date,
                transaction.Type,
                transaction.Amount,
                transaction.Note,
                transaction.AccountId,
                transaction.Account!.Name,
                transaction.CategoryId,
                transaction.Category == null ? "Uncategorized" : transaction.Category.Name,
                transaction.Category == null ? "#888888" : transaction.Category.ColorHex))
            .ToListAsync(cancellationToken);
    }

    public async Task<MonthSummary> GetMonthSummaryAsync(DateTime month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var summary = await GetSummaryAsync(new ReportFilter(start, start.AddMonths(1)), cancellationToken);
        return new MonthSummary(summary.Income, summary.Expense);
    }

    public async Task<IReadOnlyList<CategorySpend>> GetSpendByCategoryAsync(
        DateTime month,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var rows = await GetExpenseBreakdownAsync(new ReportFilter(start, start.AddMonths(1)), cancellationToken);
        return rows.Select(row => new CategorySpend(row.Name, row.ColorHex, row.Amount)).ToList();
    }

    private static async Task<List<ReportTransaction>> LoadTransactionsAsync(
        AppDbContext db,
        ReportFilter filter,
        CancellationToken cancellationToken,
        TransactionType? type = null)
    {
        var query = ApplyFilter(db.Transactions.AsNoTracking(), filter);
        if (type is { } selectedType)
            query = query.Where(transaction => transaction.Type == selectedType);

        // SQLite stores decimals as TEXT in the legacy schema. Date/account/category/type filtering
        // stays in SQLite; the small chart projection is aggregated as decimal in .NET to retain exactness.
        return await query.Select(transaction => new ReportTransaction(
                transaction.Id,
                transaction.Amount,
                transaction.Date,
                transaction.Type,
                transaction.AccountId,
                transaction.Account!.Name,
                transaction.CategoryId,
                transaction.Category == null ? "Uncategorized" : transaction.Category.Name,
                transaction.Category == null ? "#888888" : transaction.Category.ColorHex,
                transaction.Note))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<TransactionRecord> ApplyFilter(
        IQueryable<TransactionRecord> query,
        ReportFilter filter)
    {
        query = query.Where(transaction =>
            transaction.Date >= filter.StartDate && transaction.Date < filter.EndDateExclusive);

        if (filter.AccountId is { } accountId)
            query = query.Where(transaction => transaction.AccountId == accountId);
        if (filter.CategoryId is { } categoryId)
            query = query.Where(transaction => transaction.CategoryId == categoryId);

        return query;
    }
}
