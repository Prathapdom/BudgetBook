using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BudgetBook.Tests;

[TestClass]
public sealed class ReportServiceTests
{
    private string _databasePath = null!;
    private TestContextFactory _factory = null!;
    private ReportService _reports = null!;
    private int _cashAccountId;
    private int _expenseCategoryId;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"budgetbook-report-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;
        _factory = new TestContextFactory(options);

        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        var account = new Account { Name = "Cash", Currency = "USD", OpeningBalance = 100m };
        var category = new Category { Name = "Food", ColorHex = "#112233", Kind = TransactionType.Expense };
        db.AddRange(account, category);
        await db.SaveChangesAsync();
        _cashAccountId = account.Id;
        _expenseCategoryId = category.Id;
        _reports = new ReportService(_factory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
    }

    [TestMethod]
    public async Task EmptyRange_ReturnsZeroSummaryEmptyBreakdownAndZeroFilledSeries()
    {
        var filter = ReportFilter.Custom(new DateTime(2026, 2, 1), new DateTime(2026, 2, 3));

        var summary = await _reports.GetSummaryAsync(filter);
        var categories = await _reports.GetExpenseBreakdownAsync(filter);
        var series = await _reports.GetCashFlowAsync(filter, ReportGranularity.Daily);
        var recent = await _reports.GetRecentTransactionsAsync(filter);

        Assert.AreEqual(0m, summary.Income);
        Assert.AreEqual(0m, summary.Expense);
        Assert.AreEqual(0, summary.TransactionCount);
        Assert.IsEmpty(categories);
        Assert.HasCount(3, series);
        Assert.IsTrue(series.All(point => point.NetMovement == 0m && point.RunningNet == 0m));
        Assert.IsEmpty(recent);
    }

    [TestMethod]
    public async Task Summary_SeparatesIncomeAndExpenseAndExcludesTransfers()
    {
        await AddTransactionAsync(TransactionType.Income, 250m, new DateTime(2026, 3, 2));
        await AddTransactionAsync(TransactionType.Expense, 75m, new DateTime(2026, 3, 3));
        await AddTransactionAsync(TransactionType.Transfer, 40m, new DateTime(2026, 3, 4));
        var filter = ReportFilter.Custom(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        var summary = await _reports.GetSummaryAsync(filter);
        var flow = await _reports.GetCashFlowAsync(filter, ReportGranularity.Monthly);

        Assert.AreEqual(250m, summary.Income);
        Assert.AreEqual(75m, summary.Expense);
        Assert.AreEqual(175m, summary.Net);
        Assert.AreEqual(2, summary.TransactionCount);
        Assert.AreEqual(175m, flow.Single().NetMovement);
    }

    [TestMethod]
    public async Task IncomeOnlyAndExpenseOnlyRanges_ReturnIndependentTotals()
    {
        await AddTransactionAsync(TransactionType.Income, 80m, new DateTime(2026, 7, 12));
        await AddTransactionAsync(TransactionType.Expense, 30m, new DateTime(2026, 8, 12));

        var incomeOnly = await _reports.GetSummaryAsync(
            new ReportFilter(new DateTime(2026, 7, 1), new DateTime(2026, 8, 1)));
        var expenseOnly = await _reports.GetSummaryAsync(
            new ReportFilter(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1)));

        Assert.AreEqual(80m, incomeOnly.Income);
        Assert.AreEqual(0m, incomeOnly.Expense);
        Assert.AreEqual(0m, expenseOnly.Income);
        Assert.AreEqual(30m, expenseOnly.Expense);
    }

    [TestMethod]
    public async Task OptionalAccountAndCategoryFilters_AreAppliedTogether()
    {
        int secondAccountId;
        int secondCategoryId;
        await using (var db = await _factory.CreateDbContextAsync())
        {
            var account = new Account { Name = "Bank", Currency = "USD" };
            var category = new Category { Name = "Travel", Kind = TransactionType.Expense };
            db.AddRange(account, category);
            await db.SaveChangesAsync();
            secondAccountId = account.Id;
            secondCategoryId = category.Id;
        }

        await AddTransactionAsync(TransactionType.Expense, 10m, new DateTime(2026, 4, 1), _cashAccountId, _expenseCategoryId);
        await AddTransactionAsync(TransactionType.Expense, 20m, new DateTime(2026, 4, 2), _cashAccountId, secondCategoryId);
        await AddTransactionAsync(TransactionType.Expense, 30m, new DateTime(2026, 4, 3), secondAccountId, _expenseCategoryId);
        var filter = ReportFilter.Custom(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), _cashAccountId, _expenseCategoryId);

        var summary = await _reports.GetSummaryAsync(filter);
        var breakdown = await _reports.GetExpenseBreakdownAsync(filter);

        Assert.AreEqual(10m, summary.Expense);
        Assert.HasCount(1, breakdown);
        Assert.AreEqual("Food", breakdown[0].Name);
    }

    [TestMethod]
    public async Task DeletedCategory_IsReportedAsUncategorized()
    {
        await AddTransactionAsync(TransactionType.Expense, 42m, new DateTime(2026, 5, 4));
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Categories.Remove(await db.Categories.SingleAsync(category => category.Id == _expenseCategoryId));
            await db.SaveChangesAsync();
        }

        var filter = ReportFilter.Custom(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));
        var breakdown = await _reports.GetExpenseBreakdownAsync(filter);

        Assert.HasCount(1, breakdown);
        Assert.IsNull(breakdown[0].CategoryId);
        Assert.AreEqual("Uncategorized", breakdown[0].Name);
        Assert.AreEqual("#888888", breakdown[0].ColorHex);
    }

    [TestMethod]
    public async Task DateRange_UsesExclusiveEndAcrossMonthAndYearBoundaries()
    {
        await AddTransactionAsync(TransactionType.Income, 1m, new DateTime(2025, 11, 30, 23, 59, 59));
        await AddTransactionAsync(TransactionType.Income, 2m, new DateTime(2025, 12, 1));
        await AddTransactionAsync(TransactionType.Expense, 3m, new DateTime(2025, 12, 31, 23, 59, 59));
        await AddTransactionAsync(TransactionType.Income, 4m, new DateTime(2026, 1, 1));
        var filter = new ReportFilter(new DateTime(2025, 12, 1), new DateTime(2026, 1, 1));

        var summary = await _reports.GetSummaryAsync(filter);
        var lastMonth = ReportFilter.ForPeriod(ReportPeriod.LastMonth, new DateTime(2026, 1, 15));

        Assert.AreEqual(2m, summary.Income);
        Assert.AreEqual(3m, summary.Expense);
        Assert.AreEqual(new DateTime(2025, 12, 1), lastMonth.StartDate);
        Assert.AreEqual(new DateTime(2026, 1, 1), lastMonth.EndDateExclusive);
    }

    [TestMethod]
    public async Task SeriesAndComparison_CrossYearBoundaryWithoutDroppingEmptyMonths()
    {
        await AddTransactionAsync(TransactionType.Income, 100m, new DateTime(2025, 12, 10));
        await AddTransactionAsync(TransactionType.Expense, 25m, new DateTime(2026, 2, 10));
        var filter = new ReportFilter(new DateTime(2025, 12, 1), new DateTime(2026, 3, 1));

        var series = await _reports.GetIncomeExpenseAsync(filter, ReportGranularity.Monthly);
        var comparison = await _reports.GetPeriodComparisonAsync(
            new ReportFilter(new DateTime(2026, 2, 1), new DateTime(2026, 3, 1)));

        Assert.HasCount(3, series);
        Assert.AreEqual(new DateTime(2026, 1, 1), series[1].PeriodStart);
        Assert.AreEqual(0m, series[1].Income);
        Assert.AreEqual(25m, comparison.Current.Expense);
        Assert.AreEqual(0m, comparison.Previous.Expense);
    }

    [TestMethod]
    public async Task AccountDistributionAndRecentTransactions_ReturnChartReadyProjections()
    {
        await AddTransactionAsync(TransactionType.Income, 50m, new DateTime(2026, 6, 1), note: "Pay");
        await AddTransactionAsync(TransactionType.Expense, 20m, new DateTime(2026, 6, 2), note: "Lunch");
        var filter = ReportFilter.Custom(new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        var accounts = await _reports.GetAccountBalanceDistributionAsync(filter);
        var recent = await _reports.GetRecentTransactionsAsync(filter, 1);

        Assert.AreEqual(130m, accounts.Single().Balance);
        Assert.AreEqual(1m, accounts.Single().PercentageOfCurrencyTotal);
        Assert.HasCount(1, recent);
        Assert.AreEqual("Lunch", recent[0].Note);
        Assert.AreEqual("Food", recent[0].CategoryName);
    }

    [TestMethod]
    [Timeout(15000)]
    public async Task LargeMonth_TenThousandRows_RemainsExactAndLoadsWithinReleaseBudget()
    {
        await using (var db = await _factory.CreateDbContextAsync())
        {
            db.Transactions.AddRange(Enumerable.Range(0, 10_000).Select(index => new TransactionRecord
            {
                Type = index % 5 == 0 ? TransactionType.Income : TransactionType.Expense,
                Amount = 0.01m,
                Date = new DateTime(2026, 9, index % 28 + 1),
                AccountId = _cashAccountId,
                CategoryId = _expenseCategoryId,
                Note = $"Load row {index}",
            }));
            await db.SaveChangesAsync();
        }
        var filter = new ReportFilter(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));
        var stopwatch = Stopwatch.StartNew();

        var summary = await _reports.GetSummaryAsync(filter);
        var breakdown = await _reports.GetExpenseBreakdownAsync(filter);
        var recent = await _reports.GetRecentTransactionsAsync(filter, 10);
        stopwatch.Stop();

        Assert.AreEqual(20m, summary.Income);
        Assert.AreEqual(80m, summary.Expense);
        Assert.AreEqual(10_000, summary.TransactionCount);
        Assert.AreEqual(80m, breakdown.Single().Amount);
        Assert.HasCount(10, recent);
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(8), $"Large-month reports took {stopwatch.Elapsed}.");
    }

    private async Task AddTransactionAsync(
        TransactionType type,
        decimal amount,
        DateTime date,
        int? accountId = null,
        int? categoryId = null,
        string note = "")
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Transactions.Add(new TransactionRecord
        {
            Type = type,
            Amount = amount,
            Date = date,
            AccountId = accountId ?? _cashAccountId,
            CategoryId = categoryId ?? _expenseCategoryId,
            Note = note,
        });
        await db.SaveChangesAsync();
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
