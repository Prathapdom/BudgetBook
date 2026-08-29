using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BudgetBook.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BudgetBook;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }
    private object? _pendingNavigationParameter;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

        App.RootElement = Content as FrameworkElement;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
    }

    public void NavigateToTransactions(TransactionsNavigationRequest request)
    {
        _pendingNavigationParameter = request;
        var transactionsItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .First(item => string.Equals(item.Tag?.ToString(), "transactions", StringComparison.Ordinal));

        if (ReferenceEquals(NavView.SelectedItem, transactionsItem))
        {
            NavFrame.Navigate(typeof(TransactionsPage), ConsumeNavigationParameter());
            return;
        }

        NavView.SelectedItem = transactionsItem;
    }

    public void NavigateToAccountDetails(ViewModels.AccountRow row)
        => NavFrame.Navigate(typeof(AccountDetailPage), row);

    public void NavigateToBudgets()
    {
        var item = NavView.MenuItems.OfType<NavigationViewItem>()
            .First(menuItem => string.Equals(menuItem.Tag?.ToString(), "budgets", StringComparison.Ordinal));
        if (ReferenceEquals(NavView.SelectedItem, item)) NavFrame.Navigate(typeof(BudgetsPage));
        else NavView.SelectedItem = item;
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "dashboard":
                    NavFrame.Navigate(typeof(DashboardPage));
                    break;
                case "transactions":
                    NavFrame.Navigate(typeof(TransactionsPage), ConsumeNavigationParameter());
                    break;
                case "accounts":
                    NavFrame.Navigate(typeof(AccountsPage));
                    break;
                case "categories":
                    NavFrame.Navigate(typeof(CategoriesPage));
                    break;
                case "budgets":
                    NavFrame.Navigate(typeof(BudgetsPage));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}");
            }
        }
    }

    private object? ConsumeNavigationParameter()
    {
        var parameter = _pendingNavigationParameter;
        _pendingNavigationParameter = null;
        return parameter;
    }
}
