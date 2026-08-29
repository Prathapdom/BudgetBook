using BudgetBook.Data;
using BudgetBook.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BudgetBook.Pages;

public sealed partial class RecurringPage : Page
{
    public RecurringViewModel Vm { get; } = App.GetService<RecurringViewModel>();
    public RecurringPage() => InitializeComponent();
    protected override async void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); await Vm.RefreshAsync(); }
    private async void Add_Click(object sender, RoutedEventArgs e) => await EditAsync(Vm.NewDraft(), true);
    private async void Edit_Click(object sender, RoutedEventArgs e) { if (Vm.Selected is { } item) await EditAsync(Clone(item), false); }
    private async void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) { if (Vm.Selected is { } item) await EditAsync(Clone(item), false); }
    private static RecurringTransaction Clone(RecurringTransaction item) => new() { Id = item.Id, Amount = item.Amount, Type = item.Type, Note = item.Note, Tags = item.Tags, AccountId = item.AccountId, CategoryId = item.CategoryId, Frequency = item.Frequency, Interval = item.Interval, NextOccurrence = item.NextOccurrence, IsActive = item.IsActive };
    private async Task EditAsync(RecurringTransaction item, bool isNew)
    {
        var amount = new NumberBox { Header = "Amount", Minimum = .01, Value = (double)item.Amount };
        var type = new ComboBox { Header = "Type", ItemsSource = new[] { TransactionType.Expense, TransactionType.Income }, SelectedItem = item.Type == TransactionType.Transfer ? TransactionType.Expense : item.Type, HorizontalAlignment = HorizontalAlignment.Stretch };
        var account = new ComboBox { Header = "Account", ItemsSource = Vm.Accounts, DisplayMemberPath = "Name", SelectedItem = Vm.Accounts.FirstOrDefault(value => value.Id == item.AccountId), HorizontalAlignment = HorizontalAlignment.Stretch };
        var category = new ComboBox { Header = "Category", ItemsSource = Vm.Categories, DisplayMemberPath = "Name", SelectedItem = Vm.Categories.FirstOrDefault(value => value.Id == item.CategoryId), HorizontalAlignment = HorizontalAlignment.Stretch };
        var frequency = new ComboBox { Header = "Frequency", ItemsSource = Enum.GetValues(typeof(RecurrenceFrequency)), SelectedItem = item.Frequency, HorizontalAlignment = HorizontalAlignment.Stretch };
        var interval = new NumberBox { Header = "Every", Minimum = 1, Value = item.Interval };
        var next = new DatePicker { Header = "Next occurrence", Date = new DateTimeOffset(item.NextOccurrence) };
        var note = new TextBox { Header = "Note", Text = item.Note }; var tags = new TextBox { Header = "Tags", Text = item.Tags }; var active = new CheckBox { Content = "Active", IsChecked = item.IsActive };
        var panel = new StackPanel { Spacing = 10, MinWidth = 330 }; foreach (var control in new Control[] { amount, type, account, category, frequency, interval, next, note, tags, active }) panel.Children.Add(control);
        var dialog = new ContentDialog { Title = isNew ? "New recurring rule" : "Edit recurring rule", PrimaryButtonText = "Save", CloseButtonText = "Cancel", Content = panel, XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        item.Amount = (decimal)amount.Value; item.Type = (TransactionType)type.SelectedItem; item.AccountId = (account.SelectedItem as Account)?.Id ?? 0; item.CategoryId = (category.SelectedItem as Category)?.Id; item.Frequency = (RecurrenceFrequency)frequency.SelectedItem; item.Interval = Math.Max(1, (int)interval.Value); item.NextOccurrence = next.Date.Date; item.Note = note.Text.Trim(); item.Tags = tags.Text.Trim(); item.IsActive = active.IsChecked == true;
        try { await Vm.SaveAsync(item); } catch (Exception exception) { await new ContentDialog { Title = "Could not save rule", Content = exception.Message, CloseButtonText = "OK", XamlRoot = XamlRoot }.ShowAsync(); }
    }
    private async void Delete_Click(object sender, RoutedEventArgs e) { if (Vm.Selected is { } item) await Vm.DeleteAsync(item); }
}
