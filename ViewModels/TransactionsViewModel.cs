using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.ViewModels;

public partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionService _transactions;
    private readonly IAccountService _accounts;
    private readonly ICategoryService _categories;

    [ObservableProperty] private DateTime _month = DateTime.Today;
    [ObservableProperty] private TransactionRecord? _selected;

    public string MonthLabel => Month.ToString("MMMM yyyy");

    public ObservableCollection<TransactionRecord> Items { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    public TransactionsViewModel(
        ITransactionService transactions,
        IAccountService accounts,
        ICategoryService categories)
    {
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;
    }

    partial void OnMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthLabel));
        RefreshCommand.Execute(null);
    }

    [RelayCommand] private void PreviousMonth() => Month = Month.AddMonths(-1);
    [RelayCommand] private void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Items.Clear();
        foreach (var t in await _transactions.GetTransactionsAsync(Month))
            Items.Add(t);

        Accounts.Clear();
        foreach (var a in await _accounts.GetAccountsAsync()) Accounts.Add(a);

        Categories.Clear();
        foreach (var c in await _categories.GetCategoriesAsync()) Categories.Add(c);
    }

    public TransactionRecord NewDraft() => new()
    {
        Date = DateTime.Today,
        Type = TransactionType.Expense,
        AccountId = Accounts.FirstOrDefault()?.Id ?? 0,
    };

    public async Task SaveAsync(TransactionRecord txn)
    {
        await _transactions.SaveTransactionAsync(txn);
        await RefreshAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) return;
        await _transactions.DeleteTransactionAsync(Selected.Id);
        await RefreshAsync();
    }

    private bool HasSelection() => Selected is not null;

    partial void OnSelectedChanged(TransactionRecord? value) => DeleteSelectedCommand.NotifyCanExecuteChanged();
}
