using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public sealed class RecurringTransactionService(IDbContextFactory<AppDbContext> contextFactory) : IRecurringTransactionService
{
    public async Task<IReadOnlyList<RecurringTransaction>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.RecurringTransactions.AsNoTracking().Include(rule => rule.Account).Include(rule => rule.Category)
            .OrderBy(rule => rule.NextOccurrence).ToListAsync(cancellationToken);
    }

    public async Task SaveRuleAsync(RecurringTransaction rule, CancellationToken cancellationToken = default)
    {
        if (rule.Amount <= 0m) throw new InvalidOperationException("Amount must be greater than zero.");
        if (rule.Interval < 1) throw new InvalidOperationException("Interval must be at least one.");
        rule.Account = null; rule.Category = null; rule.NextOccurrence = rule.NextOccurrence.Date;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (rule.Id == 0) db.RecurringTransactions.Add(rule); else db.RecurringTransactions.Update(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRuleAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rule = await db.RecurringTransactions.FindAsync([id], cancellationToken);
        if (rule is null) return; db.RecurringTransactions.Remove(rule); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GenerateDueAsync(DateTime throughDate, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rules = await db.RecurringTransactions.Where(rule => rule.IsActive && rule.NextOccurrence <= throughDate.Date).ToListAsync(cancellationToken);
        var generated = 0;
        foreach (var rule in rules)
        {
            while (rule.NextOccurrence <= throughDate.Date)
            {
                db.Transactions.Add(new TransactionRecord { Amount = rule.Amount, Type = rule.Type, Note = rule.Note, Tags = rule.Tags, AccountId = rule.AccountId, CategoryId = rule.CategoryId, Date = rule.NextOccurrence });
                rule.NextOccurrence = Advance(rule.NextOccurrence, rule.Frequency, rule.Interval);
                generated++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return generated;
    }

    private static DateTime Advance(DateTime date, RecurrenceFrequency frequency, int interval) => frequency switch
    {
        RecurrenceFrequency.Weekly => date.AddDays(7 * interval),
        RecurrenceFrequency.Yearly => date.AddYears(interval),
        _ => date.AddMonths(interval),
    };
}
