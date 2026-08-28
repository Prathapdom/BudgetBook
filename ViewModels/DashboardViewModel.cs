using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetBook.Services;

namespace BudgetBook.ViewModels;

public class CategoryBarVm
{
    public string Name { get; init; } = "";
    public string ColorHex { get; init; } = "#888888";
    public decimal Amount { get; init; }
    /// <summary>0..1 relative to the largest category this month. Drives the bar width.</summary>
    public double Fraction { get; init; }
    public string AmountLabel => Amount.ToString("C");
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly BudgetService _budget = App.Budget;

    [ObservableProperty] private DateTime _month = DateTime.Today;
    [ObservableProperty] private decimal _income;
    [ObservableProperty] private decimal _expense;
    [ObservableProperty] private decimal _net;
    [ObservableProperty] private bool _hasSpend;

    public string MonthLabel => Month.ToString("MMMM yyyy");

    public ObservableCollection<CategoryBarVm> Spend { get; } = new();

    public DashboardViewModel() => Refresh();

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
        var s = _budget.GetMonthSummary(Month);
        Income = s.Income;
        Expense = s.Expense;
        Net = s.Net;

        Spend.Clear();
        var rows = _budget.GetSpendByCategory(Month);
        decimal max = 1m;
        foreach (var r in rows)
            if (r.Amount > max) max = r.Amount;

        foreach (var r in rows)
        {
            Spend.Add(new CategoryBarVm
            {
                Name = r.Name,
                ColorHex = r.ColorHex,
                Amount = r.Amount,
                Fraction = (double)(r.Amount / max),
            });
        }
        HasSpend = Spend.Count > 0;
    }
}
