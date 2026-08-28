using System;
using System.Collections.Generic;
using System.Linq;
using BudgetBook.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Services;

public record MonthSummary(decimal Income, decimal Expense)
{
    public decimal Net => Income - Expense;
}

public record CategorySpend(string Name, string ColorHex, decimal Amount);

/// <summary>
/// Thin data-access facade over <see cref="AppDbContext"/>. Each call opens a
/// short-lived context so the UI never holds a long-running connection.
/// </summary>
public class BudgetService
{
    public List<Account> GetAccounts()
    {
        using var db = new AppDbContext();
        return db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToList();
    }

    public List<Category> GetCategories()
    {
        using var db = new AppDbContext();
        return db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
    }

    public List<TransactionRecord> GetTransactions(DateTime? month = null, int? accountId = null)
    {
        using var db = new AppDbContext();
        IQueryable<TransactionRecord> q = db.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category);

        if (month is { } m)
        {
            var start = new DateTime(m.Year, m.Month, 1);
            var end = start.AddMonths(1);
            q = q.Where(t => t.Date >= start && t.Date < end);
        }

        if (accountId is { } aid)
            q = q.Where(t => t.AccountId == aid);

        return q.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
    }

    public MonthSummary GetMonthSummary(DateTime month)
    {
        var txns = GetTransactions(month);
        var income = txns.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = txns.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return new MonthSummary(income, expense);
    }

    public List<CategorySpend> GetSpendByCategory(DateTime month)
    {
        using var db = new AppDbContext();
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        return db.Transactions.AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.Type == TransactionType.Expense && t.Date >= start && t.Date < end)
            .AsEnumerable()
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .Select(g => new CategorySpend(
                g.Key,
                g.First().Category?.ColorHex ?? "#888888",
                g.Sum(t => t.Amount)))
            .OrderByDescending(c => c.Amount)
            .ToList();
    }

    public decimal GetAccountBalance(int accountId)
    {
        using var db = new AppDbContext();
        var acct = db.Accounts.AsNoTracking().FirstOrDefault(a => a.Id == accountId);
        if (acct is null) return 0m;

        var delta = db.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .AsEnumerable()
            .Sum(t => t.SignedAmount);

        return acct.OpeningBalance + delta;
    }

    // --- mutations -----------------------------------------------------------

    public void SaveTransaction(TransactionRecord txn)
    {
        using var db = new AppDbContext();
        if (txn.Id == 0) db.Transactions.Add(txn);
        else db.Transactions.Update(txn);
        db.SaveChanges();
    }

    public void DeleteTransaction(int id)
    {
        using var db = new AppDbContext();
        var t = db.Transactions.Find(id);
        if (t is not null) { db.Transactions.Remove(t); db.SaveChanges(); }
    }

    public void SaveAccount(Account account)
    {
        using var db = new AppDbContext();
        if (account.Id == 0) db.Accounts.Add(account);
        else db.Accounts.Update(account);
        db.SaveChanges();
    }

    public void DeleteAccount(int id)
    {
        using var db = new AppDbContext();
        var a = db.Accounts.Find(id);
        if (a is not null) { db.Accounts.Remove(a); db.SaveChanges(); }
    }

    public void SaveCategory(Category category)
    {
        using var db = new AppDbContext();
        if (category.Id == 0) db.Categories.Add(category);
        else db.Categories.Update(category);
        db.SaveChanges();
    }

    public void DeleteCategory(int id)
    {
        using var db = new AppDbContext();
        var c = db.Categories.Find(id);
        if (c is not null) { db.Categories.Remove(c); db.SaveChanges(); }
    }
}
