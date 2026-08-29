using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class BudgetService(IDbContextFactory<AppDbContext> contextFactory) : IBudgetService
{
    public async Task<IReadOnlyList<Budget>> GetBudgetsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Budgets.AsNoTracking().Include(budget => budget.Category)
            .OrderBy(budget => budget.Category!.Name).ThenBy(budget => budget.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<BudgetVsActualReport> GetBudgetReportAsync(ReportFilter filter, DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var validated = filter.Validate();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var budgets = await db.Budgets.AsNoTracking().Include(budget => budget.Category)
            .Where(budget => budget.StartDate < validated.EndDateExclusive).ToListAsync(cancellationToken);
        var earliest = budgets.Count == 0 ? validated.StartDate : budgets.Min(budget => budget.StartDate.Date);
        var transactionQuery = db.Transactions.AsNoTracking()
            .Where(transaction => transaction.Date >= earliest && transaction.Date < validated.EndDateExclusive);
        if (validated.AccountId is { } accountId) transactionQuery = transactionQuery.Where(transaction => transaction.AccountId == accountId);
        if (validated.CategoryId is { } categoryId) transactionQuery = transactionQuery.Where(transaction => transaction.CategoryId == categoryId);
        var transactions = await transactionQuery.ToListAsync(cancellationToken);
        return FinanceCalculator.CalculateBudgetReport(validated, budgets, transactions, asOfDate);
    }

    public async Task SaveBudgetAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        if (budget.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(budget), "Budget amount must be greater than zero.");
        budget.StartDate = budget.StartDate.Date;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(category => category.Id == budget.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException("Select an existing category.");
        if (category.Kind != TransactionType.Expense) throw new InvalidOperationException("Budgets can only be assigned to expense categories.");
        var duplicate = await db.Budgets.AsNoTracking().AnyAsync(existing => existing.Id != budget.Id &&
            existing.CategoryId == budget.CategoryId, cancellationToken);
        if (duplicate) throw new InvalidOperationException("This category already has a budget. Edit the existing budget instead.");
        budget.Category = null;
        if (budget.Id == 0) db.Budgets.Add(budget); else db.Budgets.Update(budget);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteBudgetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var budget = await db.Budgets.FindAsync([id], cancellationToken);
        if (budget is null) return;
        db.Budgets.Remove(budget);
        await db.SaveChangesAsync(cancellationToken);
    }
}
