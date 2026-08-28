using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class ReportService(IDbContextFactory<AppDbContext> contextFactory) : IReportService
{
    public async Task<MonthSummary> GetMonthSummaryAsync(DateTime month, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        var transactions = await db.Transactions.AsNoTracking().Where(t => t.Date >= start && t.Date < end).ToListAsync(cancellationToken);
        return FinanceCalculator.Summarize(transactions);
    }

    public async Task<IReadOnlyList<CategorySpend>> GetSpendByCategoryAsync(DateTime month, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        var transactions = await db.Transactions.AsNoTracking().Include(t => t.Category)
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start && t.Date < end)
            .ToListAsync(cancellationToken);

        return transactions.GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .Select(group => new CategorySpend(group.Key, group.First().Category?.ColorHex ?? "#888888", group.Sum(t => t.Amount)))
            .OrderByDescending(category => category.Amount)
            .ToList();
    }
}
