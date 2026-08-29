using System.Collections.ObjectModel;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudgetBook.ViewModels;

public sealed record AccountFilterOption(int? Id, string Name);

public partial class DashboardViewModel : ObservableObject
{
    private readonly IReportService _reports;
    private readonly IAccountService _accounts;
    private readonly IBudgetService _budgets;
    private bool _accountsLoaded;

    [ObservableProperty] private DateTime _month = DateTime.Today;
    [ObservableProperty] private decimal _income;
    [ObservableProperty] private decimal _expense;
    [ObservableProperty] private decimal _net;
    [ObservableProperty] private decimal _totalBalance;
    [ObservableProperty] private decimal? _savingsRate;
    [ObservableProperty] private bool _hasSpend;
    [ObservableProperty] private bool _hasRecentTransactions;
    [ObservableProperty] private bool _hasBudgets;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private AccountFilterOption? _selectedAccountFilter;

    public string MonthLabel => Month.ToString("MMMM yyyy");
    public string SavingsRateLabel => SavingsRate is { } rate ? rate.ToString("P0") : "—";

    public ObservableCollection<AccountFilterOption> AccountFilters { get; } =
        [new AccountFilterOption(null, "All accounts")];
    public ObservableCollection<CategoryBreakdown> Categories { get; } = [];
    public ObservableCollection<CashFlowPoint> CashFlow { get; } = [];
    public ObservableCollection<IncomeExpensePoint> IncomeExpense { get; } = [];
    public ObservableCollection<AccountBalanceSummary> AccountBalances { get; } = [];
    public ObservableCollection<RecentTransaction> RecentTransactions { get; } = [];
    public ObservableCollection<BudgetProgressRow> BudgetProgress { get; } = [];

    public DashboardViewModel(IReportService reports, IAccountService accounts, IBudgetService budgets)
    {
        _reports = reports;
        _accounts = accounts;
        _budgets = budgets;
        _selectedAccountFilter = AccountFilters[0];
    }

    partial void OnMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthLabel));
        RefreshCommand.Execute(null);
    }

    partial void OnSelectedAccountFilterChanged(AccountFilterOption? value)
        => RefreshCommand.Execute(null);

    partial void OnSavingsRateChanged(decimal? value) => OnPropertyChanged(nameof(SavingsRateLabel));

    [RelayCommand] private void PreviousMonth() => Month = Month.AddMonths(-1);
    [RelayCommand] private void NextMonth() => Month = Month.AddMonths(1);

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            await LoadAccountFiltersAsync();
            var filter = BuildFilter();
            var summaryTask = _reports.GetSummaryAsync(filter);
            var categoriesTask = _reports.GetExpenseBreakdownAsync(filter);
            var balancesTask = _reports.GetAccountBalanceDistributionAsync(filter);
            var cashFlowTask = _reports.GetCashFlowAsync(filter, ReportGranularity.Daily);
            var incomeExpenseTask = _reports.GetIncomeExpenseAsync(filter, ReportGranularity.Weekly);
            var recentTask = _reports.GetRecentTransactionsAsync(filter, 5);
            var budgetsTask = _budgets.GetBudgetsAsync();
            var budgetReportTask = _budgets.GetBudgetReportAsync(filter);

            await Task.WhenAll(summaryTask, categoriesTask, balancesTask, cashFlowTask, incomeExpenseTask, recentTask, budgetsTask, budgetReportTask);

            var summary = await summaryTask;
            Income = summary.Income;
            Expense = summary.Expense;
            Net = summary.Net;
            SavingsRate = summary.SavingsRate;
            Replace(Categories, await categoriesTask);
            Replace(AccountBalances, await balancesTask);
            Replace(CashFlow, await cashFlowTask);
            Replace(IncomeExpense, await incomeExpenseTask);
            Replace(RecentTransactions, await recentTask);
            var budgetDefinitions = (await budgetsTask).ToDictionary(budget => budget.Id);
            Replace(BudgetProgress, (await budgetReportTask).Items
                .Where(progress => budgetDefinitions.ContainsKey(progress.BudgetId))
                .Select(progress => new BudgetProgressRow { Budget = budgetDefinitions[progress.BudgetId], ProgressData = progress }));
            TotalBalance = AccountBalances.Sum(account => account.Balance);
            HasSpend = Categories.Count > 0;
            HasRecentTransactions = RecentTransactions.Count > 0;
            HasBudgets = BudgetProgress.Count > 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ReportFilter BuildFilter()
    {
        var start = new DateTime(Month.Year, Month.Month, 1);
        return new ReportFilter(start, start.AddMonths(1), SelectedAccountFilter?.Id);
    }

    private async Task LoadAccountFiltersAsync()
    {
        if (_accountsLoaded) return;
        var accounts = await _accounts.GetAccountsAsync();
        foreach (var account in accounts)
            AccountFilters.Add(new AccountFilterOption(account.Id, account.Name));
        _accountsLoaded = true;
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }
}
