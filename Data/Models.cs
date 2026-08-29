using System;
using System.Collections.Generic;

namespace BudgetBook.Data;

public enum TransactionType
{
    Expense = 0,
    Income = 1,
    Transfer = 2,
}

public enum BudgetPeriodType
{
    Monthly = 0,
    Weekly = 1,
}

public enum TransferDirection
{
    None = 0,
    Outgoing = 1,
    Incoming = 2,
}

public enum RecurrenceFrequency
{
    Weekly = 0,
    Monthly = 1,
    Yearly = 2,
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
    public List<Budget> Budgets { get; set; } = new();
}

public class Budget
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Amount { get; set; }
    public BudgetPeriodType PeriodType { get; set; } = BudgetPeriodType.Monthly;
    public DateTime StartDate { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public bool RolloverEnabled { get; set; }
}

public class TransactionRecord
{
    public int Id { get; set; }
    public decimal Amount { get; set; }              // always positive; sign derived from Type
    public DateTime Date { get; set; } = DateTime.Today;
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string Note { get; set; } = "";
    public string Tags { get; set; } = "";
    public string? TransferGroupId { get; set; }
    public TransferDirection TransferDirection { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Signed effect on an account balance.</summary>
    public decimal SignedAmount => Type == TransactionType.Income ||
        Type == TransactionType.Transfer && TransferDirection == TransferDirection.Incoming ? Amount : -Amount;

    // --- display helpers (not persisted) ----------------------------------
    public string DateLabel => Date.ToString("MMM dd");
    public string CategoryName => Category?.Name ?? "Uncategorized";
    public string AccountName => Account?.Name ?? "";
    public string SignedAmountLabel => SignedAmount.ToString("C");
}

public class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string Note { get; set; } = "";
    public string Tags { get; set; } = "";
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    public int Interval { get; set; } = 1;
    public DateTime NextOccurrence { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;
}

public class ExchangeRateSnapshot
{
    public int Id { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string QuoteCurrency { get; set; } = "EUR";
    public decimal Rate { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}

public class TransactionAttachment
{
    public int Id { get; set; }
    public int TransactionRecordId { get; set; }
    public TransactionRecord? Transaction { get; set; }
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
