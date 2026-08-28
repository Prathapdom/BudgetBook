using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class TransactionService(IDbContextFactory<AppDbContext> contextFactory) : ITransactionService
{
    public async Task<IReadOnlyList<TransactionRecord>> GetTransactionsAsync(DateTime? month = null, int? accountId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<TransactionRecord> query = db.Transactions.AsNoTracking().Include(t => t.Account).Include(t => t.Category);

        if (month is { } selectedMonth)
        {
            var start = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var end = start.AddMonths(1);
            query = query.Where(t => t.Date >= start && t.Date < end);
        }

        if (accountId is { } selectedAccountId)
            query = query.Where(t => t.AccountId == selectedAccountId);

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync(cancellationToken);
    }

    public async Task SaveTransactionAsync(TransactionRecord transaction, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (transaction.Id == 0) db.Transactions.Add(transaction);
        else db.Transactions.Update(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransactionAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await db.Transactions.FindAsync([id], cancellationToken);
        if (transaction is null) return;
        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }
}
