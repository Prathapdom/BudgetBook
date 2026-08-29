using BudgetBook.Data;
using BudgetBook.Reports;

namespace BudgetBook.Tests;

[TestClass]
public sealed class BudgetCalculatorTests
{
    [TestMethod]
    public void MonthlyBudget_CalculatesSpentRemainingProjectionAndThresholds()
    {
        var filter = new ReportFilter(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var category = new Category { Id = 4, Name = "Groceries", ColorHex = "#00AA00" };
        var budget = new Budget { Id = 1, CategoryId = 4, Category = category, Amount = 400m, StartDate = filter.StartDate };
        var transactions = new[]
        {
            Expense(4, 200m, new DateTime(2026, 8, 2)),
            Expense(4, 100m, new DateTime(2026, 8, 10)),
            Expense(9, 500m, new DateTime(2026, 8, 10)),
        };

        var report = FinanceCalculator.CalculateBudgetReport(filter, [budget], transactions, new DateTime(2026, 8, 15));
        var result = report.Items.Single();

        Assert.AreEqual(400m, result.EffectiveLimit);
        Assert.AreEqual(300m, result.Spent);
        Assert.AreEqual(100m, result.Remaining);
        Assert.AreEqual(BudgetHealth.Warning, result.Health);
        Assert.AreEqual(620m, result.ProjectedSpending);
    }

    [TestMethod]
    public void MonthlyRollover_AddsOnlyUnusedPriorAllocation()
    {
        var filter = new ReportFilter(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var budget = new Budget { Id = 1, CategoryId = 4, Category = new Category { Name = "Food" }, Amount = 100m, StartDate = new DateTime(2026, 6, 1), RolloverEnabled = true };
        var transactions = new[] { Expense(4, 150m, new DateTime(2026, 6, 15)), Expense(4, 20m, new DateTime(2026, 8, 5)) };

        var result = FinanceCalculator.CalculateBudgetReport(filter, [budget], transactions, new DateTime(2026, 8, 31)).Items.Single();

        Assert.AreEqual(150m, result.EffectiveLimit);
        Assert.AreEqual(130m, result.Remaining);
        Assert.AreEqual(BudgetHealth.Normal, result.Health);
    }

    [TestMethod]
    public void WeeklyBudget_ScalesToWeeksIntersectingSelectedMonth()
    {
        var filter = new ReportFilter(new DateTime(2026, 2, 1), new DateTime(2026, 3, 1));
        var budget = new Budget { Id = 2, CategoryId = 7, Category = new Category { Name = "Dining" }, Amount = 50m, PeriodType = BudgetPeriodType.Weekly, StartDate = filter.StartDate };

        var result = FinanceCalculator.CalculateBudgetReport(filter, [budget], [], new DateTime(2026, 2, 28)).Items.Single();

        Assert.AreEqual(200m, result.EffectiveLimit);
        Assert.AreEqual(0m, result.ProjectedSpending);
    }

    [TestMethod]
    public void BudgetHealth_UsesSeventyFiveNinetyAndOneHundredPercentThresholds()
    {
        var filter = new ReportFilter(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var budget = new Budget { Id = 1, CategoryId = 4, Category = new Category { Name = "Food" }, Amount = 100m, StartDate = filter.StartDate };
        BudgetHealth Health(decimal spent) => FinanceCalculator.CalculateBudgetReport(filter, [budget],
            [Expense(4, spent, filter.StartDate)], new DateTime(2026, 8, 31)).Items.Single().Health;

        Assert.AreEqual(BudgetHealth.Normal, Health(74m));
        Assert.AreEqual(BudgetHealth.Warning, Health(75m));
        Assert.AreEqual(BudgetHealth.Critical, Health(90m));
        Assert.AreEqual(BudgetHealth.Exceeded, Health(100m));
    }

    private static TransactionRecord Expense(int categoryId, decimal amount, DateTime date)
        => new() { CategoryId = categoryId, Amount = amount, Date = date, Type = TransactionType.Expense };
}
