using BudgetBook.Data;

namespace BudgetBook.Reports;

public record MonthSummary(decimal Income, decimal Expense)
{
    public decimal Net => Income - Expense;
}

public record CategorySpend(string Name, string ColorHex, decimal Amount);

/// <summary>Pure finance calculations shared by reports and unit tests.</summary>
public static class FinanceCalculator
{
    public static BudgetVsActualReport CalculateBudgetReport(
        ReportFilter filter,
        IEnumerable<Budget> budgets,
        IEnumerable<TransactionRecord> transactions,
        DateTime? asOfDate = null)
    {
        var validated = filter.Validate();
        var allTransactions = transactions.ToList();
        var reportDay = (asOfDate ?? DateTime.Today).Date;
        var totalDays = validated.DayCount;
        var elapsedDays = reportDay < validated.StartDate ? 0
            : reportDay >= validated.EndDateExclusive ? totalDays
            : Math.Clamp((reportDay - validated.StartDate).Days + 1, 1, totalDays);

        var items = budgets
            .Where(budget => budget.StartDate.Date < validated.EndDateExclusive)
            .Select(budget =>
            {
                var spent = allTransactions.Where(transaction => transaction.Type == TransactionType.Expense &&
                    transaction.CategoryId == budget.CategoryId && transaction.Date >= validated.StartDate &&
                    transaction.Date < validated.EndDateExclusive).Sum(transaction => transaction.Amount);
                var baseLimit = budget.PeriodType == BudgetPeriodType.Weekly
                    ? budget.Amount * (decimal)Math.Ceiling(totalDays / 7d)
                    : budget.Amount * CountMonths(validated.StartDate, validated.EndDateExclusive);
                var carry = budget.RolloverEnabled && budget.PeriodType == BudgetPeriodType.Monthly
                    ? CalculateMonthlyCarry(budget, validated.StartDate, allTransactions)
                    : 0m;
                var effectiveLimit = baseLimit + carry;
                var projected = elapsedDays == 0 ? 0m : spent / elapsedDays * totalDays;
                return new BudgetProgress(budget.Id, budget.CategoryId, budget.Category?.Name ?? "Category",
                    budget.Category?.ColorHex ?? "#888888", baseLimit, effectiveLimit, spent, projected,
                    budget.PeriodType, budget.RolloverEnabled);
            })
            .OrderByDescending(item => item.Progress)
            .ThenBy(item => item.CategoryName)
            .ToList();

        return new BudgetVsActualReport(validated.StartDate, validated.EndDateExclusive, items);
    }

    private static int CountMonths(DateTime start, DateTime endExclusive)
        => Math.Max(1, (endExclusive.Year - start.Year) * 12 + endExclusive.Month - start.Month);

    private static decimal CalculateMonthlyCarry(Budget budget, DateTime reportStart, IEnumerable<TransactionRecord> transactions)
    {
        var budgetStart = new DateTime(budget.StartDate.Year, budget.StartDate.Month, 1);
        var reportMonth = new DateTime(reportStart.Year, reportStart.Month, 1);
        var completedMonths = Math.Max(0, (reportMonth.Year - budgetStart.Year) * 12 + reportMonth.Month - budgetStart.Month);
        if (completedMonths == 0) return 0m;
        var priorSpent = transactions.Where(transaction => transaction.Type == TransactionType.Expense &&
            transaction.CategoryId == budget.CategoryId && transaction.Date >= budgetStart && transaction.Date < reportMonth)
            .Sum(transaction => transaction.Amount);
        return Math.Max(0m, budget.Amount * completedMonths - priorSpent);
    }

    public static MonthSummary Summarize(IEnumerable<TransactionRecord> transactions)
    {
        var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return new MonthSummary(income, expense);
    }

    public static decimal CalculateBalance(decimal openingBalance, IEnumerable<TransactionRecord> transactions)
        => openingBalance + transactions.Sum(t => t.SignedAmount);

    public static DashboardSummary Summarize(ReportFilter filter, IEnumerable<ReportTransaction> transactions)
    {
        var validated = filter.Validate();
        var materialized = transactions.Where(IsFinancial).ToList();
        return new DashboardSummary(
            validated.StartDate,
            validated.EndDateExclusive,
            materialized.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            materialized.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            materialized.Count);
    }

    public static IReadOnlyList<CategoryBreakdown> ExpenseBreakdown(IEnumerable<ReportTransaction> transactions)
    {
        var groups = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new
            {
                t.CategoryId,
                Name = string.IsNullOrWhiteSpace(t.CategoryName) ? "Uncategorized" : t.CategoryName,
                Color = string.IsNullOrWhiteSpace(t.CategoryColorHex) ? "#888888" : t.CategoryColorHex,
            })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.Name,
                group.Key.Color,
                Amount = group.Sum(t => t.Amount),
            })
            .OrderByDescending(row => row.Amount)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = groups.Sum(row => row.Amount);
        return groups.Select(row => new CategoryBreakdown(
            row.CategoryId,
            row.Name,
            row.Color,
            row.Amount,
            total == 0m ? 0m : row.Amount / total)).ToList();
    }

    public static IReadOnlyList<IncomeExpensePoint> IncomeExpenseSeries(
        ReportFilter filter,
        IEnumerable<ReportTransaction> transactions,
        ReportGranularity granularity = ReportGranularity.Auto)
    {
        var validated = filter.Validate();
        var resolved = ResolveGranularity(validated, granularity);
        var grouped = transactions
            .Where(IsFinancial)
            .GroupBy(t => BucketStart(t.Date, resolved))
            .ToDictionary(
                group => group.Key,
                group => (
                    Income: group.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expense: group.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)));

        return EnumerateBuckets(validated, resolved)
            .Select(start => grouped.TryGetValue(start, out var values)
                ? new IncomeExpensePoint(start, values.Income, values.Expense)
                : new IncomeExpensePoint(start, 0m, 0m))
            .ToList();
    }

    public static IReadOnlyList<CashFlowPoint> CashFlowSeries(
        ReportFilter filter,
        IEnumerable<ReportTransaction> transactions,
        ReportGranularity granularity = ReportGranularity.Auto)
    {
        decimal running = 0m;
        return IncomeExpenseSeries(filter, transactions, granularity)
            .Select(point =>
            {
                running += point.Net;
                return new CashFlowPoint(point.PeriodStart, point.Net, running);
            })
            .ToList();
    }

    public static ReportGranularity ResolveGranularity(ReportFilter filter, ReportGranularity requested)
    {
        if (requested != ReportGranularity.Auto) return requested;
        return filter.DayCount switch
        {
            <= 45 => ReportGranularity.Daily,
            <= 180 => ReportGranularity.Weekly,
            _ => ReportGranularity.Monthly,
        };
    }

    private static bool IsFinancial(ReportTransaction transaction)
        => transaction.Type is TransactionType.Income or TransactionType.Expense;

    private static DateTime BucketStart(DateTime date, ReportGranularity granularity)
        => granularity switch
        {
            ReportGranularity.Daily => date.Date,
            ReportGranularity.Weekly => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            ReportGranularity.Monthly => new DateTime(date.Year, date.Month, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity)),
        };

    private static IEnumerable<DateTime> EnumerateBuckets(ReportFilter filter, ReportGranularity granularity)
    {
        var cursor = BucketStart(filter.StartDate, granularity);
        while (cursor < filter.EndDateExclusive)
        {
            yield return cursor;
            cursor = granularity switch
            {
                ReportGranularity.Daily => cursor.AddDays(1),
                ReportGranularity.Weekly => cursor.AddDays(7),
                ReportGranularity.Monthly => cursor.AddMonths(1),
                _ => throw new ArgumentOutOfRangeException(nameof(granularity)),
            };
        }
    }
}
