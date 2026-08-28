using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services;

namespace BudgetBook.ViewModels;

public class AccountRow
{
    public Account Account { get; init; } = new();
    public decimal Balance { get; init; }
    public string BalanceLabel => Balance.ToString("C");
}

public partial class AccountsViewModel : ObservableObject
{
    private readonly BudgetService _budget = App.Budget;

    public ObservableCollection<AccountRow> Items { get; } = new();

    [ObservableProperty] private AccountRow? _selected;

    public AccountsViewModel() => Refresh();

    [RelayCommand]
    public void Refresh()
    {
        Items.Clear();
        foreach (var a in _budget.GetAccounts())
            Items.Add(new AccountRow { Account = a, Balance = _budget.GetAccountBalance(a.Id) });
    }

    public Account NewDraft() => new() { Name = "", OpeningBalance = 0m, Currency = "USD" };

    public void Save(Account a)
    {
        _budget.SaveAccount(a);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (Selected is null) return;
        _budget.DeleteAccount(Selected.Account.Id);
        Refresh();
    }

    private bool HasSelection() => Selected is not null;

    partial void OnSelectedChanged(AccountRow? value) => DeleteSelectedCommand.NotifyCanExecuteChanged();
}
