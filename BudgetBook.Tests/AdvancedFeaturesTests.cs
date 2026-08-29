using BudgetBook.Data;
using BudgetBook.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BudgetBook.Tests;

[TestClass]
public sealed class AdvancedFeaturesTests
{
    private string? _path;
    private TestFactory CreateFactory()
    {
        _path = Path.Combine(Path.GetTempPath(), $"budgetbook-advanced-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={_path}").Options;
        var factory = new TestFactory(options); AppDbContext.Initialize(factory); return factory;
    }

    [TestCleanup]
    public void Cleanup() { SqliteConnection.ClearAllPools(); if (_path is not null && File.Exists(_path)) File.Delete(_path); }

    [TestMethod]
    public async Task Transfer_CreatesBalancedLinkedEntries_AndDeletesAsAUnit()
    {
        var factory = CreateFactory(); int source; int destination;
        await using (var db = factory.CreateDbContext()) { var accounts = await db.Accounts.OrderBy(item => item.Id).Take(2).ToListAsync(); source = accounts[0].Id; destination = accounts[1].Id; }
        var service = new TransactionService(factory);

        await service.SaveTransferAsync(source, destination, 125m, new DateTime(2026, 8, 10), "Move funds", "internal");
        var entries = (await service.GetTransactionsAsync()).Where(item => item.Type == TransactionType.Transfer).ToList();

        Assert.HasCount(2, entries);
        Assert.AreEqual(0m, entries.Sum(item => item.SignedAmount));
        Assert.AreEqual(entries[0].TransferGroupId, entries[1].TransferGroupId);
        await service.DeleteTransactionAsync(entries[0].Id);
        Assert.IsFalse((await service.GetTransactionsAsync()).Any(item => item.TransferGroupId == entries[0].TransferGroupId));
    }

    [TestMethod]
    public async Task RecurringRule_GeneratesEveryDueOccurrence_AndAdvancesNextDate()
    {
        var factory = CreateFactory(); int accountId; int categoryId;
        await using (var db = factory.CreateDbContext()) { accountId = await db.Accounts.Select(item => item.Id).FirstAsync(); categoryId = await db.Categories.Where(item => item.Kind == TransactionType.Expense).Select(item => item.Id).FirstAsync(); }
        var service = new RecurringTransactionService(factory);
        await service.SaveRuleAsync(new RecurringTransaction { Amount = 50m, AccountId = accountId, CategoryId = categoryId, Note = "Subscription", Frequency = RecurrenceFrequency.Monthly, NextOccurrence = new DateTime(2026, 1, 1), IsActive = true });

        var count = await service.GenerateDueAsync(new DateTime(2026, 3, 1));

        Assert.AreEqual(3, count);
        Assert.AreEqual(new DateTime(2026, 4, 1), (await service.GetRulesAsync()).Single().NextOccurrence);
    }

    [TestMethod]
    public async Task ExchangeRate_UsesLatestDirectOrInverseSnapshot()
    {
        var factory = CreateFactory(); var service = new ExchangeRateService(factory);
        await service.SaveRateAsync(new ExchangeRateSnapshot { BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = .8m, CapturedAt = new DateTime(2026, 1, 1) });
        await service.SaveRateAsync(new ExchangeRateSnapshot { BaseCurrency = "USD", QuoteCurrency = "EUR", Rate = .9m, CapturedAt = new DateTime(2026, 2, 1) });

        Assert.AreEqual(90m, await service.ConvertAsync(100m, "USD", "EUR", new DateTime(2026, 2, 2)));
        Assert.AreEqual(100m, await service.ConvertAsync(90m, "EUR", "USD", new DateTime(2026, 2, 2)));
    }

    [TestMethod]
    public async Task PdfReport_WritesAValidMonthlyDocument()
    {
        var factory = CreateFactory(); int accountId; int categoryId;
        await using (var db = factory.CreateDbContext())
        {
            accountId = await db.Accounts.Select(item => item.Id).FirstAsync();
            categoryId = await db.Categories.Where(item => item.Kind == TransactionType.Expense).Select(item => item.Id).FirstAsync();
            db.Transactions.Add(new TransactionRecord { AccountId = accountId, CategoryId = categoryId, Amount = 84.25m, Date = new DateTime(2026, 8, 12), Type = TransactionType.Expense, Note = "Weekly groceries", Tags = "household" });
            db.Budgets.Add(new Budget { CategoryId = categoryId, Amount = 400m, StartDate = new DateTime(2026, 8, 1) });
            await db.SaveChangesAsync();
        }
        var output = Environment.GetEnvironmentVariable("BUDGETBOOK_PDF_QA_PATH") ?? Path.Combine(Path.GetTempPath(), $"budgetbook-report-{Guid.NewGuid():N}.pdf");
        var service = new PdfReportService(new ReportService(factory), new TransactionService(factory), new BudgetService(factory));

        await service.ExportMonthlyReportAsync(output, new DateTime(2026, 8, 1));

        var bytes = await File.ReadAllBytesAsync(output);
        CollectionAssert.AreEqual("%PDF"u8.ToArray(), bytes.Take(4).ToArray());
        Assert.IsTrue(bytes.Length > 500);
        if (Environment.GetEnvironmentVariable("BUDGETBOOK_PDF_QA_PATH") is null) File.Delete(output);
    }

    private sealed class TestFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
