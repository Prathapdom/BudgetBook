using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryService _categories;

    public ObservableCollection<Category> Items { get; } = new();

    [ObservableProperty] private Category? _selected;

    public CategoriesViewModel(ICategoryService categories) => _categories = categories;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Items.Clear();
        foreach (var c in await _categories.GetCategoriesAsync())
            Items.Add(c);
    }

    public Category NewDraft() => new() { Name = "", ColorHex = "#0078D4", Kind = TransactionType.Expense };

    public async Task SaveAsync(Category category)
    {
        await _categories.SaveCategoryAsync(category);
        await RefreshAsync();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) return;
        await _categories.DeleteCategoryAsync(Selected.Id);
        await RefreshAsync();
    }

    private bool HasSelection() => Selected is not null;

    partial void OnSelectedChanged(Category? value) => DeleteSelectedCommand.NotifyCanExecuteChanged();
}
