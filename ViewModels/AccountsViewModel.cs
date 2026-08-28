using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.ViewModels;

public class AccountRow
{
    public Account Account { get; init; } = new();
    public decimal Balance { get; init; }
    public string BalanceLabel => Balance.ToString("C");
}

public partial class AccountsViewModel : ObservableObject
{
    private readonly IAccountService _accounts;

    public ObservableCollection<AccountRow> Items { get; } = new();

    [ObservableProperty] private AccountRow? _selected;

    public AccountsViewModel(IAccountService accounts) => _accounts = accounts;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Items.Clear();
        foreach (var account in await _accounts.GetAccountsAsync())
            Items.Add(new AccountRow
            {
                Account = account,
                Balance = await _accounts.GetAccountBalanceAsync(account.Id),
            });
    }

    public Account NewDraft() => new() { Name = "", OpeningBalance = 0m, Currency = "USD" };

    public async Task SaveAsync(Account account)
    {
        await _accounts.SaveAccountAsync(account);
        await RefreshAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) return;
        await _accounts.DeleteAccountAsync(Selected.Account.Id);
        await RefreshAsync();
    }

    private bool HasSelection() => Selected is not null;

    partial void OnSelectedChanged(AccountRow? value) => DeleteSelectedCommand.NotifyCanExecuteChanged();
}
