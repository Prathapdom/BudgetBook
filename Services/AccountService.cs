using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class AccountService(IDbContextFactory<AppDbContext> contextFactory) : IAccountService
{
    public async Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAccountBalanceAsync(int accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (account is null) return 0m;

        var transactions = await db.Transactions.AsNoTracking().Where(t => t.AccountId == accountId).ToListAsync(cancellationToken);
        return FinanceCalculator.CalculateBalance(account.OpeningBalance, transactions);
    }

    public async Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (account.Id == 0) db.Accounts.Add(account);
        else db.Accounts.Update(account);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.Accounts.FindAsync([id], cancellationToken);
        if (account is null) return;
        db.Accounts.Remove(account);
        await db.SaveChangesAsync(cancellationToken);
    }
}
