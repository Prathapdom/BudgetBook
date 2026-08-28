using System;
using System.Collections.Generic;

namespace BudgetBook.Data;

public enum TransactionType
{
    Expense = 0,
    Income = 1,
    Transfer = 2,
}

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal OpeningBalance { get; set; }
    public string Currency { get; set; } = "USD";

    public List<TransactionRecord> Transactions { get; set; } = new();
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Glyph { get; set; } = ""; // Segoe Fluent icon
    public string ColorHex { get; set; } = "#0078D4";
    public TransactionType Kind { get; set; } = TransactionType.Expense;

    public List<TransactionRecord> Transactions { get; set; } = new();
}

public class TransactionRecord
{
    public int Id { get; set; }
    public decimal Amount { get; set; }              // always positive; sign derived from Type
    public DateTime Date { get; set; } = DateTime.Today;
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string Note { get; set; } = "";

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Signed effect on an account balance.</summary>
    public decimal SignedAmount => Type == TransactionType.Income ? Amount : -Amount;

    // --- display helpers (not persisted) ----------------------------------
    public string DateLabel => Date.ToString("MMM dd");
    public string CategoryName => Category?.Name ?? "Uncategorized";
    public string AccountName => Account?.Name ?? "";
    public string SignedAmountLabel => SignedAmount.ToString("C");
}
