using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BudgetBook.Data;
using BudgetBook.Reports;
using BudgetBook.Services.Interfaces;

namespace BudgetBook.ViewModels;

public partial class AccountDetailViewModel(ITransactionService transactions, IReportService reports) : ObservableObject
{
    [ObservableProperty] private AccountRow? _account;
    public ObservableCollection<TransactionRecord> Transactions { get; } = new();
    public ObservableCollection<CashFlowPoint> Trend { get; } = new();
    public string BalanceLabel => Account?.BalanceLabel ?? string.Empty;

    public async Task LoadAsync(AccountRow row)
    {
        Account = row;
        OnPropertyChanged(nameof(BalanceLabel));
        Transactions.Clear();
        foreach (var transaction in await transactions.GetTransactionsAsync(accountId: row.Account.Id)) Transactions.Add(transaction);
        Trend.Clear();
        var end = DateTime.Today.Date.AddDays(1);
        var filter = new ReportFilter(end.AddMonths(-6), end, row.Account.Id);
        foreach (var point in await reports.GetCashFlowAsync(filter, ReportGranularity.Weekly)) Trend.Add(point);
    }
}
