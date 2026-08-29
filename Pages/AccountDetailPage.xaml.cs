using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class AccountDetailPage : Page
{
    public AccountDetailViewModel Vm { get; } = App.GetService<AccountDetailViewModel>();
    public AccountDetailPage() => InitializeComponent();
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is AccountRow row) await Vm.LoadAsync(row);
    }
    private void ViewTransactions_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Account is { } row) MainWindow.Instance?.NavigateToTransactions(new TransactionsNavigationRequest(DateTime.Today, AccountId: row.Account.Id, FilterLabel: $"{row.Account.Name} transactions"));
    }
}
