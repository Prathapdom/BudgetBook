using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.ViewModels;

public sealed class CategoryRow
{
    public Category Category { get; init; } = new();
    public decimal MonthlyAmount { get; init; }
    public int TransactionCount { get; init; }
    public string ActivityLabel => $"{MonthlyAmount:C} this month · {TransactionCount} transaction{(TransactionCount == 1 ? "" : "s")}";
}

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryService _categories;
    private readonly ITransactionService _transactions;
    public ObservableCollection<CategoryRow> ExpenseItems { get; } = new();
    public ObservableCollection<CategoryRow> IncomeItems { get; } = new();
    [ObservableProperty] private CategoryRow? _selected;

    public CategoriesViewModel(ICategoryService categories, ITransactionService transactions)
    {
        _categories = categories;
        _transactions = transactions;
    }

    public async Task RefreshAsync()
    {
        ExpenseItems.Clear(); IncomeItems.Clear();
        var monthTransactions = await _transactions.GetTransactionsAsync(DateTime.Today);
        foreach (var category in await _categories.GetCategoriesAsync())
        {
            var activity = monthTransactions.Where(t => t.CategoryId == category.Id).ToList();
            var row = new CategoryRow { Category = category, MonthlyAmount = activity.Sum(t => t.Amount), TransactionCount = activity.Count };
            (category.Kind == TransactionType.Income ? IncomeItems : ExpenseItems).Add(row);
        }
    }

    public Category NewDraft() => new() { Name = "", ColorHex = "#0078D4", Glyph = "\uE8C7", Kind = TransactionType.Expense };
    public async Task SaveAsync(Category category) { await _categories.SaveCategoryAsync(category); await RefreshAsync(); }
    public async Task DeleteAsync(CategoryRow row) { await _categories.DeleteCategoryAsync(row.Category.Id); Selected = null; await RefreshAsync(); }
}
