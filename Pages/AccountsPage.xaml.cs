using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.Data;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class AccountsPage : Page
{
    public AccountsViewModel Vm { get; }

    public AccountsPage()
    {
        Vm = App.GetService<AccountsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await Vm.RefreshAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
        => await ShowEditorAsync(Vm.NewDraft(), isNew: true);

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Account), isNew: false);
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is { } row) MainWindow.Instance?.NavigateToAccountDetails(row);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is not { } row) return;
        var warning = row.TransactionCount == 0
            ? "This account has no transactions."
            : $"This will also permanently delete {row.TransactionCount} transaction{(row.TransactionCount == 1 ? "" : "s")} linked to this account.";
        var result = await new ContentDialog
        {
            Title = $"Delete {row.Account.Name}?",
            Content = warning,
            PrimaryButtonText = "Delete account",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        }.ShowAsync();
        if (result == ContentDialogResult.Primary) await Vm.DeleteAsync(row);
    }

    private void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (Vm.Selected is { } row) MainWindow.Instance?.NavigateToAccountDetails(row);
    }

    private static Account Clone(Account a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        OpeningBalance = a.OpeningBalance,
        Currency = a.Currency,
    };

    private async Task ShowEditorAsync(Account draft, bool isNew)
    {
        var nameBox = new TextBox { Header = "Name", Text = draft.Name };
        var openingBox = new NumberBox
        {
            Header = "Opening balance",
            Value = (double)draft.OpeningBalance,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
        };
        var currencyBox = new TextBox { Header = "Currency code", Text = draft.Currency };

        var panel = new StackPanel { Spacing = 12, MinWidth = 300 };
        panel.Children.Add(nameBox);
        panel.Children.Add(openingBox);
        panel.Children.Add(currencyBox);

        var dialog = new ContentDialog
        {
            Title = isNew ? "New account" : "Edit account",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) return;

        draft.Name = nameBox.Text.Trim();
        draft.OpeningBalance = (decimal)(double.IsNaN(openingBox.Value) ? 0 : openingBox.Value);
        draft.Currency = string.IsNullOrWhiteSpace(currencyBox.Text) ? "USD" : currencyBox.Text.Trim().ToUpperInvariant();

        await Vm.SaveAsync(draft);
    }
}
