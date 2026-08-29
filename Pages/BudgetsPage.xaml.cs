using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.Data;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class BudgetsPage : Page
{
    public BudgetsViewModel Vm { get; } = App.GetService<BudgetsViewModel>();
    public Array PeriodTypes { get; } = Enum.GetValues(typeof(BudgetPeriodType));
    public BudgetsPage() => InitializeComponent();
    protected override async void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); await Vm.RefreshAsync(); }
    private async void Add_Click(object sender, RoutedEventArgs e) => await ShowEditorAsync(Vm.NewDraft(), true);
    private async void Edit_Click(object sender, RoutedEventArgs e) { if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Budget), false); }
    private async void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) { if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Budget), false); }
    private static Budget Clone(Budget budget) => new() { Id = budget.Id, CategoryId = budget.CategoryId, Amount = budget.Amount, PeriodType = budget.PeriodType, StartDate = budget.StartDate, RolloverEnabled = budget.RolloverEnabled };

    private async Task ShowEditorAsync(Budget draft, bool isNew)
    {
        var categoryBox = new ComboBox { Header = "Expense category", ItemsSource = Vm.ExpenseCategories, DisplayMemberPath = nameof(Category.Name), SelectedItem = Vm.ExpenseCategories.FirstOrDefault(category => category.Id == draft.CategoryId), HorizontalAlignment = HorizontalAlignment.Stretch };
        var amountBox = new NumberBox { Header = "Limit", Minimum = 0.01, Value = (double)draft.Amount, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        var periodBox = new ComboBox { Header = "Period", ItemsSource = PeriodTypes, SelectedItem = draft.PeriodType, HorizontalAlignment = HorizontalAlignment.Stretch };
        var startPicker = new DatePicker { Header = "Starts", Date = new DateTimeOffset(draft.StartDate) };
        var rollover = new CheckBox { Content = "Carry unused monthly budget forward", IsChecked = draft.RolloverEnabled };
        var panel = new StackPanel { Spacing = 12, MinWidth = 330 };
        foreach (var control in new Control[] { categoryBox, amountBox, periodBox, startPicker, rollover }) panel.Children.Add(control);
        var dialog = new ContentDialog { Title = isNew ? "New budget" : "Edit budget", PrimaryButtonText = "Save", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary, Content = panel, XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        draft.CategoryId = (categoryBox.SelectedItem as Category)?.Id ?? 0;
        draft.Amount = (decimal)(double.IsNaN(amountBox.Value) ? 0 : amountBox.Value);
        draft.PeriodType = periodBox.SelectedItem is BudgetPeriodType type ? type : BudgetPeriodType.Monthly;
        draft.StartDate = startPicker.Date.Date;
        draft.RolloverEnabled = rollover.IsChecked == true && draft.PeriodType == BudgetPeriodType.Monthly;
        try { await Vm.SaveAsync(draft); }
        catch (Exception exception) { await new ContentDialog { Title = "Budget could not be saved", Content = exception.Message, CloseButtonText = "OK", XamlRoot = XamlRoot }.ShowAsync(); }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is not { } row) return;
        var result = await new ContentDialog { Title = $"Delete {row.CategoryName} budget?", Content = "Transactions are not affected.", PrimaryButtonText = "Delete", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close, XamlRoot = XamlRoot }.ShowAsync();
        if (result == ContentDialogResult.Primary) await Vm.DeleteAsync(row);
    }
}
