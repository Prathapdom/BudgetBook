using BudgetBook.Data;

namespace BudgetBook.Reports;

public enum ReportPeriod
{
    ThisMonth,
    LastMonth,
    Last3Months,
    Last6Months,
    ThisYear,
    Custom,
}

public enum ReportGranularity
{
    Auto,
    Daily,
    Weekly,
    Monthly,
}

/// <summary>
/// One date range and its optional dimensions, shared by every report query.
/// EndDateExclusive makes month and year boundaries unambiguous.
/// </summary>
public sealed record ReportFilter(
    DateTime StartDate,
    DateTime EndDateExclusive,
    int? AccountId = null,
    int? CategoryId = null)
{
    public int DayCount => (EndDateExclusive.Date - StartDate.Date).Days;

    public static ReportFilter ForPeriod(ReportPeriod period, DateTime? today = null)
    {
        var date = (today ?? DateTime.Today).Date;
        var monthStart = new DateTime(date.Year, date.Month, 1);

        return period switch
        {
            ReportPeriod.ThisMonth => new(monthStart, monthStart.AddMonths(1)),
            ReportPeriod.LastMonth => new(monthStart.AddMonths(-1), monthStart),
            ReportPeriod.Last3Months => new(monthStart.AddMonths(-2), monthStart.AddMonths(1)),
            ReportPeriod.Last6Months => new(monthStart.AddMonths(-5), monthStart.AddMonths(1)),
            ReportPeriod.ThisYear => new(new DateTime(date.Year, 1, 1), new DateTime(date.Year + 1, 1, 1)),
            ReportPeriod.Custom => throw new ArgumentException("Custom reports require explicit start and end dates.", nameof(period)),
            _ => throw new ArgumentOutOfRangeException(nameof(period)),
        };
    }

    public static ReportFilter Custom(DateTime startDate, DateTime endDateInclusive, int? accountId = null, int? categoryId = null)
        => new ReportFilter(startDate.Date, endDateInclusive.Date.AddDays(1), accountId, categoryId).Validate();

    public ReportFilter Validate()
    {
        if (EndDateExclusive.Date <= StartDate.Date)
            throw new ArgumentException("The report end date must be later than its start date.");

        return this with { StartDate = StartDate.Date, EndDateExclusive = EndDateExclusive.Date };
    }
}

public sealed record DashboardSummary(
    DateTime StartDate,
    DateTime EndDateExclusive,
    decimal Income,
    decimal Expense,
    int TransactionCount)
{
    public decimal Net => Income - Expense;
    public decimal? SavingsRate => Income == 0m ? null : Net / Income;
}

public sealed record CategoryBreakdown(
    int? CategoryId,
    string Name,
    string ColorHex,
    decimal Amount,
    decimal Percentage);

public sealed record CashFlowPoint(
    DateTime PeriodStart,
    decimal NetMovement,
    decimal RunningNet);

public sealed record IncomeExpensePoint(
    DateTime PeriodStart,
    decimal Income,
    decimal Expense)
{
    public decimal Net => Income - Expense;
}

public sealed record AccountBalanceSummary(
    int AccountId,
    string Name,
    string Currency,
    decimal OpeningBalance,
    decimal Balance,
    decimal PercentageOfCurrencyTotal);

public sealed record PeriodComparison(
    DashboardSummary Current,
    DashboardSummary Previous)
{
    public decimal IncomeChange => Current.Income - Previous.Income;
    public decimal ExpenseChange => Current.Expense - Previous.Expense;
    public decimal NetChange => Current.Net - Previous.Net;
    public decimal? IncomeChangeRate => ChangeRate(Current.Income, Previous.Income);
    public decimal? ExpenseChangeRate => ChangeRate(Current.Expense, Previous.Expense);

    private static decimal? ChangeRate(decimal current, decimal previous)
        => previous == 0m ? null : (current - previous) / previous;
}

public sealed record RecentTransaction(
    int Id,
    DateTime Date,
    TransactionType Type,
    decimal Amount,
    string Note,
    int AccountId,
    string AccountName,
    int? CategoryId,
    string CategoryName,
    string CategoryColorHex)
{
    public decimal SignedAmount => Type == TransactionType.Income ? Amount : -Amount;
}

/// <summary>Minimal transaction projection used by pure report aggregation.</summary>
public sealed record ReportTransaction(
    int Id,
    decimal Amount,
    DateTime Date,
    TransactionType Type,
    int AccountId,
    string AccountName,
    int? CategoryId,
    string CategoryName,
    string CategoryColorHex,
    string Note = "");
