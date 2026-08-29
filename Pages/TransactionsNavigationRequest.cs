using BudgetBook.Data;

namespace BudgetBook.Pages;

public sealed record TransactionsNavigationRequest(
    DateTime Month,
    int? AccountId = null,
    int? CategoryId = null,
    string? FilterLabel = null,
    bool UncategorizedOnly = false,
    bool OpenNewTransaction = false,
    TransactionRecord? Draft = null);
