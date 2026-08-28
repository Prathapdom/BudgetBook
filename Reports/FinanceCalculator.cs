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
