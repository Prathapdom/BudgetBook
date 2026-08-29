using BudgetBook.Data;
using BudgetBook.Pages;
using BudgetBook.Services.Interfaces;
using BudgetBook.ViewModels;

namespace BudgetBook.Tests;

[TestClass]
public sealed class TransactionsViewModelTests
{
    [TestMethod]
    public async Task NavigationRequest_AppliesCategoryAndAccountFilters_AndCanClearThem()
    {
        var month = new DateTime(2026, 8, 1);
        var transactions = new[]
        {
            Transaction(1, 10, 1, month.AddDays(1)),
            Transaction(2, 10, 2, month.AddDays(2)),
            Transaction(3, 20, 2, month.AddDays(3)),
        };
        var viewModel = new TransactionsViewModel(
            new FakeTransactionService(transactions),
            new FakeAccountService(),
            new FakeCategoryService());

        viewModel.ApplyNavigationRequest(new TransactionsNavigationRequest(
            month, AccountId: 10, CategoryId: 2, FilterLabel: "Groceries transactions"));
        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasFilter);
        Assert.AreEqual("Groceries transactions", viewModel.FilterLabel);
        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual(2, viewModel.Items[0].Id);

        await viewModel.ClearFilterCommand.ExecuteAsync(null);

        Assert.IsFalse(viewModel.HasFilter);
        Assert.HasCount(3, viewModel.Items);
    }

    [TestMethod]
    public async Task SearchTypeAndDateGrouping_ComposeWithoutChangingLoadedMonth()
    {
        var month = new DateTime(2026, 8, 1);
        var groceries = Transaction(1, 10, 2, month.AddDays(4));
        groceries.Type = TransactionType.Expense;
        groceries.Note = "Weekly market";
        groceries.Account = new Account { Id = 10, Name = "Checking" };
        groceries.Category = new Category { Id = 2, Name = "Groceries" };
        var salary = Transaction(2, 10, 3, month.AddDays(4));
        salary.Type = TransactionType.Income;
        salary.Note = "Payday";
        salary.Account = groceries.Account;
        salary.Category = new Category { Id = 3, Name = "Salary" };

        var viewModel = new TransactionsViewModel(
            new FakeTransactionService([groceries, salary]), new FakeAccountService(), new FakeCategoryService());
        viewModel.ApplyNavigationRequest(new TransactionsNavigationRequest(month));
        await viewModel.RefreshAsync();

        Assert.HasCount(1, viewModel.Groups);
        Assert.HasCount(2, viewModel.Groups[0]);

        viewModel.SearchText = "market";
        viewModel.TypeFilter = TransactionType.Expense;

        Assert.HasCount(1, viewModel.Items);
        Assert.AreEqual(groceries.Id, viewModel.Items[0].Id);
        Assert.IsTrue(viewModel.HasFilter);
    }

    private static TransactionRecord Transaction(int id, int accountId, int? categoryId, DateTime date)
        => new() { Id = id, AccountId = accountId, CategoryId = categoryId, Date = date, Amount = id };

    private sealed class FakeTransactionService(IEnumerable<TransactionRecord> transactions) : ITransactionService
    {
        private readonly IReadOnlyList<TransactionRecord> _transactions = transactions.ToList();

        public Task<IReadOnlyList<TransactionRecord>> GetTransactionsAsync(
            DateTime? month = null,
            int? accountId = null,
            CancellationToken cancellationToken = default)
        {
            var result = _transactions.Where(transaction =>
                    (month is null || transaction.Date.Year == month.Value.Year && transaction.Date.Month == month.Value.Month)
                    && (accountId is null || transaction.AccountId == accountId))
                .ToList();
            return Task.FromResult<IReadOnlyList<TransactionRecord>>(result);
        }

        public Task SaveTransactionAsync(TransactionRecord transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteTransactionAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAccountService : IAccountService
    {
        public Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Account>>([]);
        public Task<decimal> GetAccountBalanceAsync(int accountId, CancellationToken cancellationToken = default) => Task.FromResult(0m);
        public Task SaveAccountAsync(Account account, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAccountAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCategoryService : ICategoryService
    {
        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task SaveCategoryAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
