using System.Collections.ObjectModel;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BudgetBook.ViewModels;

public partial class RecurringViewModel(IRecurringTransactionService recurring, IAccountService accounts, ICategoryService categories) : ObservableObject
{
    public ObservableCollection<RecurringTransaction> Items { get; } = [];
    public ObservableCollection<Account> Accounts { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    [ObservableProperty] private RecurringTransaction? _selected;

    public async Task RefreshAsync()
    {
        await recurring.GenerateDueAsync(DateTime.Today);
        Items.Clear(); foreach (var item in await recurring.GetRulesAsync()) Items.Add(item);
        Accounts.Clear(); foreach (var item in await accounts.GetAccountsAsync()) Accounts.Add(item);
        Categories.Clear(); foreach (var item in await categories.GetCategoriesAsync()) Categories.Add(item);
    }
    public RecurringTransaction NewDraft() => new() { AccountId = Accounts.FirstOrDefault()?.Id ?? 0, NextOccurrence = DateTime.Today, IsActive = true };
    public async Task SaveAsync(RecurringTransaction item) { await recurring.SaveRuleAsync(item); await RefreshAsync(); }
    public async Task DeleteAsync(RecurringTransaction item) { await recurring.DeleteRuleAsync(item.Id); Selected = null; await RefreshAsync(); }
}
