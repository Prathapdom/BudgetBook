using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services;

namespace BudgetBook.ViewModels;

public partial class TransactionsViewModel : ObservableObject
{
    private readonly BudgetService _budget = App.Budget;

    [ObservableProperty] private DateTime _month = DateTime.Today;
    [ObservableProperty] private TransactionRecord? _selected;

    public string MonthLabel => Month.ToString("MMMM yyyy");

    public ObservableCollection<TransactionRecord> Items { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    public TransactionsViewModel() => Refresh();

    partial void OnMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthLabel));
        Refresh();
    }

    [RelayCommand] private void PreviousMonth() => Month = Month.AddMonths(-1);
    [RelayCommand] private void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    public void Refresh()
    {
        Items.Clear();
        foreach (var t in _budget.GetTransactions(Month))
            Items.Add(t);

        Accounts.Clear();
        foreach (var a in _budget.GetAccounts()) Accounts.Add(a);

        Categories.Clear();
        foreach (var c in _budget.GetCategories()) Categories.Add(c);
    }

    public TransactionRecord NewDraft() => new()
    {
        Date = DateTime.Today,
        Type = TransactionType.Expense,
        AccountId = Accounts.FirstOrDefault()?.Id ?? 0,
    };

    public void Save(TransactionRecord txn)
    {
        _budget.SaveTransaction(txn);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (Selected is null) return;
        _budget.DeleteTransaction(Selected.Id);
        Refresh();
    }

    private bool HasSelection() => Selected is not null;

    partial void OnSelectedChanged(TransactionRecord? value) => DeleteSelectedCommand.NotifyCanExecuteChanged();
}
