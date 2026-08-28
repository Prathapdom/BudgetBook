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
    public AccountsViewModel Vm { get; } = new();

    public AccountsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Vm.Refresh();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
        => await ShowEditorAsync(Vm.NewDraft(), isNew: true);

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Account), isNew: false);
    }

    private async void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Account), isNew: false);
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

        Vm.Save(draft);
    }
}
