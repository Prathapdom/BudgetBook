using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using BudgetBook.Pages;

namespace BudgetBook.ViewModels;

public sealed record TransactionFilterOption(int? Id, string Name)
{
    public override string ToString() => Name;
}

public sealed class TransactionGroup(DateTime date) : ObservableCollection<TransactionRecord>
{
    public DateTime Date { get; } = date;
    public string Header => Date.Date == DateTime.Today ? "Today" :
        Date.Date == DateTime.Today.AddDays(-1) ? "Yesterday" : Date.ToString("dddd, MMMM d");
}

public partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionService _transactions;
    private readonly IAccountService _accounts;
    private readonly ICategoryService _categories;
    private readonly List<TransactionRecord> _loadedItems = [];

    [ObservableProperty] private DateTime _month = DateTime.Today;
    [ObservableProperty] private TransactionRecord? _selected;
    [ObservableProperty] private int? _accountFilterId;
    [ObservableProperty] private int? _categoryFilterId;
    [ObservableProperty] private bool _uncategorizedOnly;
    [ObservableProperty] private string _filterLabel = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private TransactionType? _typeFilter;
    private bool _suppressRefresh;

    public string MonthLabel => Month.ToString("MMMM yyyy");
    public bool HasFilter => AccountFilterId is not null || CategoryFilterId is not null ||
        UncategorizedOnly || TypeFilter is not null || !string.IsNullOrWhiteSpace(SearchText);

    public ObservableCollection<TransactionRecord> Items { get; } = new();
    public ObservableCollection<TransactionGroup> Groups { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<TransactionFilterOption> AccountFilters { get; } = new();
    public ObservableCollection<TransactionFilterOption> CategoryFilters { get; } = new();

    public TransactionsViewModel(ITransactionService transactions, IAccountService accounts, ICategoryService categories)
    {
        _transactions = transactions;
        _accounts = accounts;
        _categories = categories;
    }

    partial void OnMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthLabel));
        if (!_suppressRefresh) RefreshCommand.Execute(null);
    }

    partial void OnAccountFilterIdChanged(int? value) { OnPropertyChanged(nameof(HasFilter)); ApplyClientFilters(); }
    partial void OnCategoryFilterIdChanged(int? value) { OnPropertyChanged(nameof(HasFilter)); ApplyClientFilters(); }
    partial void OnUncategorizedOnlyChanged(bool value) { OnPropertyChanged(nameof(HasFilter)); ApplyClientFilters(); }
    partial void OnSearchTextChanged(string value) { OnPropertyChanged(nameof(HasFilter)); ApplyClientFilters(); }
    partial void OnTypeFilterChanged(TransactionType? value) { OnPropertyChanged(nameof(HasFilter)); ApplyClientFilters(); }

    [RelayCommand] private void PreviousMonth() => Month = Month.AddMonths(-1);
    [RelayCommand] private void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    public async Task RefreshAsync()
    {
        _loadedItems.Clear();
        _loadedItems.AddRange(await _transactions.GetTransactionsAsync(Month));

        Accounts.Clear();
        foreach (var account in await _accounts.GetAccountsAsync()) Accounts.Add(account);
        Categories.Clear();
        foreach (var category in await _categories.GetCategoriesAsync()) Categories.Add(category);

        AccountFilters.Clear();
        AccountFilters.Add(new(null, "All accounts"));
        foreach (var account in Accounts) AccountFilters.Add(new(account.Id, account.Name));
        CategoryFilters.Clear();
        CategoryFilters.Add(new(null, "All categories"));
        foreach (var category in Categories) CategoryFilters.Add(new(category.Id, category.Name));

        ApplyClientFilters();
    }

    private void ApplyClientFilters()
    {
        if (_loadedItems.Count == 0 && Items.Count == 0) return;
        var search = SearchText.Trim();
        var filtered = _loadedItems.Where(transaction =>
            (AccountFilterId is null || transaction.AccountId == AccountFilterId) &&
            (UncategorizedOnly ? transaction.CategoryId is null : CategoryFilterId is null || transaction.CategoryId == CategoryFilterId) &&
            (TypeFilter is null || transaction.Type == TypeFilter) &&
            (search.Length == 0 || transaction.Note.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
             transaction.AccountName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
             transaction.CategoryName.Contains(search, StringComparison.CurrentCultureIgnoreCase)))
            .OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.Id).ToList();

        Items.Clear();
        foreach (var transaction in filtered) Items.Add(transaction);
        Groups.Clear();
        foreach (var dateGroup in filtered.GroupBy(transaction => transaction.Date.Date))
        {
            var group = new TransactionGroup(dateGroup.Key);
            foreach (var transaction in dateGroup) group.Add(transaction);
            Groups.Add(group);
        }
    }

    public void ApplyNavigationRequest(TransactionsNavigationRequest request)
    {
        _suppressRefresh = true;
        Month = request.Month;
        AccountFilterId = request.AccountId;
        CategoryFilterId = request.CategoryId;
        UncategorizedOnly = request.UncategorizedOnly;
        FilterLabel = request.FilterLabel ?? "Filtered transactions";
        _suppressRefresh = false;
    }

    [RelayCommand]
    private Task ClearFilterAsync()
    {
        AccountFilterId = null;
        CategoryFilterId = null;
        UncategorizedOnly = false;
        TypeFilter = null;
        SearchText = string.Empty;
        FilterLabel = string.Empty;
        ApplyClientFilters();
        return Task.CompletedTask;
    }

    public TransactionRecord NewDraft() => new()
    {
        Date = DateTime.Today, Type = TransactionType.Expense, AccountId = Accounts.FirstOrDefault()?.Id ?? 0,
    };

    public async Task SaveAsync(TransactionRecord transaction)
    {
        await _transactions.SaveTransactionAsync(transaction);
        await RefreshAsync();
    }

    public async Task DeleteAsync(TransactionRecord transaction)
    {
        await _transactions.DeleteTransactionAsync(transaction.Id);
        Selected = null;
        await RefreshAsync();
    }

    public async Task RestoreAsync(TransactionRecord transaction)
    {
        transaction.Id = 0;
        transaction.Account = null;
        transaction.Category = null;
        await _transactions.SaveTransactionAsync(transaction);
        await RefreshAsync();
    }
}
