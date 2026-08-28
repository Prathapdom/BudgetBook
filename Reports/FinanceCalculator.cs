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
}
