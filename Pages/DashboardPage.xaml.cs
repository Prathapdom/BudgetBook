using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel Vm { get; } = new();

    public DashboardPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Vm.Refresh();
    }
}
