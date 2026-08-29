using System.Collections.ObjectModel;
using BudgetBook.Controls.Cards;
using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudgetBook.ViewModels;

public sealed class BudgetProgressRow
{
    public Budget Budget { get; init; } = new();
    public BudgetProgress ProgressData { get; init; } = null!;
    public string CategoryName => ProgressData.CategoryName;
    public string SpentLabel => $"{ProgressData.Spent:C} spent · {ProgressData.ProjectedSpending:C} projected";
    public string LimitLabel => $"Limit {ProgressData.EffectiveLimit:C}";
    public string RemainingLabel => ProgressData.Remaining >= 0 ? $"{ProgressData.Remaining:C} left" : $"{Math.Abs(ProgressData.Remaining):C} over";
    public double Progress => (double)Math.Clamp(ProgressData.Progress, 0m, 1m);
    public BudgetProgressStatus Status => ProgressData.Health switch
    {
        BudgetHealth.Warning => BudgetProgressStatus.Warning,
        BudgetHealth.Critical => BudgetProgressStatus.Critical,
        BudgetHealth.Exceeded => BudgetProgressStatus.Exceeded,
        _ => BudgetProgressStatus.Normal,
    };
    public string PeriodLabel => Budget.PeriodType == BudgetPeriodType.Monthly ? "Monthly" : "Weekly";
}

public partial class BudgetsViewModel : ObservableObject
{
    private readonly IBudgetService _budgets;
    private readonly ICategoryService _categories;
    [ObservableProperty] private DateTime _month = DateTime.Today;
    [ObservableProperty] private BudgetProgressRow? _selected;
    [ObservableProperty] private decimal _totalBudget;
    [ObservableProperty] private decimal _totalSpent;
    [ObservableProperty] private decimal _totalProjected;
    public string MonthLabel => Month.ToString("MMMM yyyy");
    public string TotalBudgetLabel => TotalBudget.ToString("C");
    public string TotalSpentLabel => TotalSpent.ToString("C");
    public string TotalProjectedLabel => TotalProjected.ToString("C");
    public ObservableCollection<BudgetProgressRow> Items { get; } = [];
    public ObservableCollection<Category> ExpenseCategories { get; } = [];

    public BudgetsViewModel(IBudgetService budgets, ICategoryService categories) { _budgets = budgets; _categories = categories; }
    partial void OnMonthChanged(DateTime value) { OnPropertyChanged(nameof(MonthLabel)); RefreshCommand.Execute(null); }
    [RelayCommand] private void PreviousMonth() => Month = Month.AddMonths(-1);
    [RelayCommand] private void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var start = new DateTime(Month.Year, Month.Month, 1);
        var budgetsTask = _budgets.GetBudgetsAsync();
        var reportTask = _budgets.GetBudgetReportAsync(new ReportFilter(start, start.AddMonths(1)));
        var categoryTask = _categories.GetCategoriesAsync();
        await Task.WhenAll(budgetsTask, reportTask, categoryTask);
        var budgets = (await budgetsTask).ToDictionary(budget => budget.Id);
        var report = await reportTask;
        Items.Clear();
        foreach (var progress in report.Items)
            if (budgets.TryGetValue(progress.BudgetId, out var budget)) Items.Add(new BudgetProgressRow { Budget = budget, ProgressData = progress });
        ExpenseCategories.Clear();
        foreach (var category in (await categoryTask).Where(category => category.Kind == TransactionType.Expense)) ExpenseCategories.Add(category);
        TotalBudget = report.TotalBudget; TotalSpent = report.TotalSpent; TotalProjected = report.TotalProjected;
        OnPropertyChanged(nameof(TotalBudgetLabel)); OnPropertyChanged(nameof(TotalSpentLabel)); OnPropertyChanged(nameof(TotalProjectedLabel));
    }

    public Budget NewDraft() => new() { Amount = 0m, CategoryId = ExpenseCategories.FirstOrDefault()?.Id ?? 0, PeriodType = BudgetPeriodType.Monthly, StartDate = new DateTime(Month.Year, Month.Month, 1) };
    public async Task SaveAsync(Budget budget) { await _budgets.SaveBudgetAsync(budget); await RefreshAsync(); }
    public async Task DeleteAsync(BudgetProgressRow row) { await _budgets.DeleteBudgetAsync(row.Budget.Id); Selected = null; await RefreshAsync(); }
}
