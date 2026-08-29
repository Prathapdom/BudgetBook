using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.ViewModels;

public class AccountRow
{
    public Account Account { get; init; } = new();
    public decimal Balance { get; init; }
    public int TransactionCount { get; init; }
    public decimal Allocation { get; set; }
    public string BalanceLabel => $"{Account.Currency} {Balance:N2}";
    public string Subtitle => $"{Allocation:P0} of total · {TransactionCount} transaction{(TransactionCount == 1 ? "" : "s")}";
}

public partial class AccountsViewModel : ObservableObject
{
    private readonly IAccountService _accounts;
    private readonly ITransactionService _transactions;
    public ObservableCollection<AccountRow> Items { get; } = new();
    public ObservableCollection<AccountBalanceSummary> Distribution { get; } = new();
    [ObservableProperty] private AccountRow? _selected;
    [ObservableProperty] private decimal _totalBalance;
    public string TotalBalanceLabel => TotalBalance.ToString("C");

    public AccountsViewModel(IAccountService accounts, ITransactionService transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task RefreshAsync()
    {
        Items.Clear();
        Distribution.Clear();
        var accounts = await _accounts.GetAccountsAsync();
        var transactions = await _transactions.GetTransactionsAsync();
        var rows = new List<AccountRow>();
        foreach (var account in accounts)
        {
            var accountTransactions = transactions.Where(t => t.AccountId == account.Id).ToList();
            rows.Add(new AccountRow
            {
                Account = account,
                Balance = account.OpeningBalance + accountTransactions.Sum(t => t.SignedAmount),
                TransactionCount = accountTransactions.Count,
            });
        }

        TotalBalance = rows.Sum(row => row.Balance);
        OnPropertyChanged(nameof(TotalBalanceLabel));
        var positiveTotal = rows.Where(row => row.Balance > 0).Sum(row => row.Balance);
        foreach (var row in rows)
        {
            row.Allocation = positiveTotal == 0 ? 0 : Math.Max(0, row.Balance) / positiveTotal;
            Items.Add(row);
            Distribution.Add(new AccountBalanceSummary(row.Account.Id, row.Account.Name, row.Account.Currency,
                row.Account.OpeningBalance, row.Balance, row.Allocation));
        }
    }

    public Account NewDraft() => new() { Name = "", OpeningBalance = 0m, Currency = "USD" };
    public async Task SaveAsync(Account account) { await _accounts.SaveAccountAsync(account); await RefreshAsync(); }
    public async Task DeleteAsync(AccountRow row) { await _accounts.DeleteAccountAsync(row.Account.Id); Selected = null; await RefreshAsync(); }
}
