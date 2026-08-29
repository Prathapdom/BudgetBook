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
        transaction.Account = null;
        transaction.Category = null;
        if (transaction.Id == 0) db.Transactions.Add(transaction);
        else if (!string.IsNullOrWhiteSpace(transaction.TransferGroupId))
        {
            var linked = await db.Transactions.Where(item => item.TransferGroupId == transaction.TransferGroupId).ToListAsync(cancellationToken);
            foreach (var item in linked)
            {
                item.Amount = transaction.Amount;
                item.Date = transaction.Date;
                item.Note = transaction.Note;
                item.Tags = transaction.Tags;
            }
        }
        else db.Transactions.Update(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveTransferAsync(int sourceAccountId, int destinationAccountId, decimal amount, DateTime date, string note, string tags = "", CancellationToken cancellationToken = default)
    {
        if (sourceAccountId == destinationAccountId) throw new InvalidOperationException("Source and destination accounts must be different.");
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Transfer amount must be greater than zero.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accountCount = await db.Accounts.CountAsync(account => account.Id == sourceAccountId || account.Id == destinationAccountId, cancellationToken);
        if (accountCount != 2) throw new InvalidOperationException("Both transfer accounts must exist.");
        var groupId = Guid.NewGuid().ToString("N");
        db.Transactions.AddRange(
            new TransactionRecord { Type = TransactionType.Transfer, TransferDirection = TransferDirection.Outgoing, TransferGroupId = groupId, AccountId = sourceAccountId, Amount = amount, Date = date.Date, Note = note, Tags = tags },
            new TransactionRecord { Type = TransactionType.Transfer, TransferDirection = TransferDirection.Incoming, TransferGroupId = groupId, AccountId = destinationAccountId, Amount = amount, Date = date.Date, Note = note, Tags = tags });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransactionAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var transaction = await db.Transactions.FindAsync([id], cancellationToken);
        if (transaction is null) return;
        if (!string.IsNullOrWhiteSpace(transaction.TransferGroupId))
            db.Transactions.RemoveRange(await db.Transactions.Where(item => item.TransferGroupId == transaction.TransferGroupId).ToListAsync(cancellationToken));
        else db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(cancellationToken);
    }
}
