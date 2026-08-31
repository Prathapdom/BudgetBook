using BudgetBook.Data;
using BudgetBook.Reports;

namespace BudgetBook.Tests;

[TestClass]
public sealed class FinanceCalculatorTests
{
    [TestMethod]
    public void Summarize_SeparatesIncomeAndExpense_AndExcludesTransfers()
    {
        var transactions = new[]
        {
            Transaction(TransactionType.Income, 2_500.75m),
            Transaction(TransactionType.Expense, 450.25m),
            Transaction(TransactionType.Expense, 49.50m),
            Transaction(TransactionType.Transfer, 300m),
        };

        var summary = FinanceCalculator.Summarize(transactions);

        Assert.AreEqual(2_500.75m, summary.Income);
        Assert.AreEqual(499.75m, summary.Expense);
        Assert.AreEqual(2_001.00m, summary.Net);
    }

    [TestMethod]
    public void CalculateBalance_AppliesSignedTransactionAmountsToOpeningBalance()
    {
        var transactions = new[]
        {
            Transaction(TransactionType.Income, 200m),
            Transaction(TransactionType.Expense, 75.25m),
            Transaction(TransactionType.Transfer, 25m),
        };

        var balance = FinanceCalculator.CalculateBalance(1_000m, transactions);

        Assert.AreEqual(1_099.75m, balance);
    }

    [TestMethod]
    public void DecimalCalculations_RemainExactAcrossManySmallValuesAndLinkedTransfers()
    {
        var transactions = Enumerable.Range(0, 1_000)
            .Select(_ => Transaction(TransactionType.Expense, 0.01m)).ToList();
        transactions.Add(new TransactionRecord { Type = TransactionType.Transfer, TransferDirection = TransferDirection.Outgoing, Amount = 123.45m });
        transactions.Add(new TransactionRecord { Type = TransactionType.Transfer, TransferDirection = TransferDirection.Incoming, Amount = 123.45m });

        var balance = FinanceCalculator.CalculateBalance(100m, transactions);

        Assert.AreEqual(90m, balance);
    }

    private static TransactionRecord Transaction(TransactionType type, decimal amount)
        => new() { Type = type, Amount = amount };
}
