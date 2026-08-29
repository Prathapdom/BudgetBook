using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.ViewModels;
using BudgetBook.Controls.Charts;
using BudgetBook.Reports;
using Microsoft.UI.Xaml;

namespace BudgetBook.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel Vm { get; }

    public DashboardPage()
    {
        Vm = App.GetService<DashboardViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await Vm.RefreshAsync();
    }

    private void QuickAdd_Click(object sender, RoutedEventArgs e)
        => MainWindow.Instance?.NavigateToTransactions(new TransactionsNavigationRequest(
            Vm.Month,
            AccountId: Vm.SelectedAccountFilter?.Id,
            OpenNewTransaction: true));

    private void CategoryChart_CategorySelected(object sender, CategorySelectedEventArgs e)
        => MainWindow.Instance?.NavigateToTransactions(new TransactionsNavigationRequest(
            Vm.Month,
            AccountId: Vm.SelectedAccountFilter?.Id,
            CategoryId: e.CategoryId,
            FilterLabel: $"{e.Name} transactions",
            UncategorizedOnly: e.CategoryId is null));

    private void AccountButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AccountBalanceSummary account }) return;
        MainWindow.Instance?.NavigateToTransactions(new TransactionsNavigationRequest(
            Vm.Month,
            AccountId: account.AccountId,
            FilterLabel: $"{account.Name} transactions"));
    }

    private void ManageBudgets_Click(object sender, RoutedEventArgs e)
        => MainWindow.Instance?.NavigateToBudgets();
}
