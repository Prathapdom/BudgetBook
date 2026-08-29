using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;
using BudgetBook.Data;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class CategoriesPage : Page
{
    private sealed record IconOption(string Name, string Glyph) { public override string ToString() => $"{Glyph}  {Name}"; }
    private static readonly IconOption[] Icons =
    [
        new("Wallet", "\uE8C7"), new("Groceries", "\uE7BF"), new("Home", "\uE80F"),
        new("Transport", "\uE806"), new("Dining", "\uE8D4"), new("Utilities", "\uE945"),
        new("Entertainment", "\uE7FC"), new("Salary", "\uE8C7"), new("Health", "\uE95E"), new("Shopping", "\uE7BF"),
    ];

    public CategoriesViewModel Vm { get; }
    public CategoriesPage() { Vm = App.GetService<CategoriesViewModel>(); InitializeComponent(); }
    protected override async void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); await Vm.RefreshAsync(); }

    private async void Add_Click(object sender, RoutedEventArgs e) => await ShowEditorAsync(Vm.NewDraft(), true);
    private async void Edit_Click(object sender, RoutedEventArgs e) { if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Category), false); }
    private async void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) { if (Vm.Selected is { } row) await ShowEditorAsync(Clone(row.Category), false); }
    private void ExpenseList_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (ExpenseList.SelectedItem is not null) IncomeList.SelectedItem = null; }
    private void IncomeList_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IncomeList.SelectedItem is not null) ExpenseList.SelectedItem = null; }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is not { } row) return;
        var message = row.TransactionCount == 0 ? "This category is not in use." : $"{row.TransactionCount} linked transaction{(row.TransactionCount == 1 ? "" : "s")} will be kept and marked Uncategorized.";
        var result = await new ContentDialog { Title = $"Delete {row.Category.Name}?", Content = message, PrimaryButtonText = "Delete category", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close, XamlRoot = XamlRoot }.ShowAsync();
        if (result == ContentDialogResult.Primary) await Vm.DeleteAsync(row);
    }

    private static Category Clone(Category category) => new() { Id = category.Id, Name = category.Name, Glyph = category.Glyph, ColorHex = category.ColorHex, Kind = category.Kind };

    private async Task ShowEditorAsync(Category draft, bool isNew)
    {
        var nameBox = new TextBox { Header = "Name", Text = draft.Name };
        var iconBox = new ComboBox { Header = "Icon", ItemsSource = Icons, SelectedItem = Icons.FirstOrDefault(icon => icon.Glyph == draft.Glyph) ?? Icons[0], HorizontalAlignment = HorizontalAlignment.Stretch };
        var colorPicker = new ColorPicker { Color = HexToBrushConverter.ParseColor(draft.ColorHex), IsAlphaEnabled = false, IsColorChannelTextInputVisible = true, IsHexInputVisible = true };
        var kindBox = new ComboBox { Header = "Kind", ItemsSource = Enum.GetValues(typeof(TransactionType)), SelectedItem = draft.Kind, HorizontalAlignment = HorizontalAlignment.Stretch };
        var panel = new StackPanel { Spacing = 12, MinWidth = 330 };
        panel.Children.Add(nameBox); panel.Children.Add(iconBox); panel.Children.Add(new TextBlock { Text = "Color" }); panel.Children.Add(colorPicker); panel.Children.Add(kindBox);
        var dialog = new ContentDialog { Title = isNew ? "New category" : "Edit category", PrimaryButtonText = "Save", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary, Content = panel, XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(nameBox.Text)) return;
        draft.Name = nameBox.Text.Trim();
        draft.Glyph = (iconBox.SelectedItem as IconOption)?.Glyph ?? Icons[0].Glyph;
        var color = colorPicker.Color;
        draft.ColorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        draft.Kind = kindBox.SelectedItem is TransactionType type ? type : TransactionType.Expense;
        await Vm.SaveAsync(draft);
    }
}
