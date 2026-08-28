using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.Data;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class TransactionsPage : Page
{
    public TransactionsViewModel Vm { get; } = new();

    public TransactionsPage()
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
        if (Vm.Selected is { } t) await ShowEditorAsync(Clone(t), isNew: false);
    }

    private async void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (Vm.Selected is { } t) await ShowEditorAsync(Clone(t), isNew: false);
    }

    private static TransactionRecord Clone(TransactionRecord t) => new()
    {
        Id = t.Id,
        Amount = t.Amount,
        Date = t.Date,
        Type = t.Type,
        Note = t.Note,
        AccountId = t.AccountId,
        CategoryId = t.CategoryId,
    };

    private async System.Threading.Tasks.Task ShowEditorAsync(TransactionRecord draft, bool isNew)
    {
        var typeBox = new ComboBox
        {
            Header = "Type",
            ItemsSource = Enum.GetValues(typeof(TransactionType)),
            SelectedItem = draft.Type,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var amountBox = new NumberBox
        {
            Header = "Amount",
            Value = (double)draft.Amount,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Minimum = 0,
        };
        var datePicker = new DatePicker
        {
            Header = "Date",
            Date = new DateTimeOffset(draft.Date),
        };
        var accountBox = new ComboBox
        {
            Header = "Account",
            ItemsSource = Vm.Accounts,
            DisplayMemberPath = nameof(Account.Name),
            SelectedItem = Vm.Accounts.FirstOrDefault(a => a.Id == draft.AccountId) ?? Vm.Accounts.FirstOrDefault(),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var categoryBox = new ComboBox
        {
            Header = "Category",
            ItemsSource = Vm.Categories,
            DisplayMemberPath = nameof(Category.Name),
            SelectedItem = Vm.Categories.FirstOrDefault(c => c.Id == draft.CategoryId),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var noteBox = new TextBox { Header = "Note", Text = draft.Note };

        var panel = new StackPanel { Spacing = 12, MinWidth = 320 };
        panel.Children.Add(typeBox);
        panel.Children.Add(amountBox);
        panel.Children.Add(datePicker);
        panel.Children.Add(accountBox);
        panel.Children.Add(categoryBox);
        panel.Children.Add(noteBox);

        var dialog = new ContentDialog
        {
            Title = isNew ? "New transaction" : "Edit transaction",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        draft.Type = (TransactionType)typeBox.SelectedItem;
        draft.Amount = (decimal)(double.IsNaN(amountBox.Value) ? 0 : amountBox.Value);
        draft.Date = datePicker.Date.DateTime.Date;
        draft.AccountId = (accountBox.SelectedItem as Account)?.Id ?? Vm.Accounts.FirstOrDefault()?.Id ?? 0;
        draft.CategoryId = (categoryBox.SelectedItem as Category)?.Id;
        draft.Note = noteBox.Text?.Trim() ?? "";

        if (draft.AccountId == 0)
        {
            await new ContentDialog
            {
                Title = "Add an account first",
                Content = "Create an account on the Accounts page before recording transactions.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot,
            }.ShowAsync();
            return;
        }

        Vm.Save(draft);
    }
}
