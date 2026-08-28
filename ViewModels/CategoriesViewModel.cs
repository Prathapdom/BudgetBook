using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Data;
using BudgetBook.Services;

namespace BudgetBook.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly BudgetService _budget = App.Budget;

    public ObservableCollection<Category> Items { get; } = new();

    [ObservableProperty] private Category? _selected;

    public CategoriesViewModel() => Refresh();

    [RelayCommand]
    public void Refresh()
    {
        Items.Clear();
        foreach (var c in _budget.GetCategories())
            Items.Add(c);
    }

    public Category NewDraft() => new() { Name = "", ColorHex = "#0078D4", Kind = TransactionType.Expense };

    public void Save(Category c)
    {
        _budget.SaveCategory(c);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (Selected is null) return;
        _budget.DeleteCategory(Selected.Id);
        Refresh();
    }

    private bool HasSelection() => Selected is not null;

    partial void OnSelectedChanged(Category? value) => DeleteSelectedCommand.NotifyCanExecuteChanged();
}
