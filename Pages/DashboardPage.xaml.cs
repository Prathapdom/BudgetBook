using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.ViewModels;

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
}
