using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BudgetBook.Data;
using BudgetBook.ViewModels;

namespace BudgetBook.Pages;

public sealed partial class CategoriesPage : Page
{
    public CategoriesViewModel Vm { get; } = new();

    public CategoriesPage()
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
        if (Vm.Selected is { } c) await ShowEditorAsync(Clone(c), isNew: false);
    }

    private async void List_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (Vm.Selected is { } c) await ShowEditorAsync(Clone(c), isNew: false);
    }

    private static Category Clone(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Glyph = c.Glyph,
        ColorHex = c.ColorHex,
        Kind = c.Kind,
    };

    private async Task ShowEditorAsync(Category draft, bool isNew)
    {
        var nameBox = new TextBox { Header = "Name", Text = draft.Name };
        var colorBox = new TextBox { Header = "Color (hex, e.g. #2E7D32)", Text = draft.ColorHex };
        var kindBox = new ComboBox
        {
            Header = "Kind",
            ItemsSource = Enum.GetValues(typeof(TransactionType)),
            SelectedItem = draft.Kind,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var panel = new StackPanel { Spacing = 12, MinWidth = 300 };
        panel.Children.Add(nameBox);
        panel.Children.Add(colorBox);
        panel.Children.Add(kindBox);

        var dialog = new ContentDialog
        {
            Title = isNew ? "New category" : "Edit category",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = panel,
            XamlRoot = XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) return;

        draft.Name = nameBox.Text.Trim();
        draft.ColorHex = string.IsNullOrWhiteSpace(colorBox.Text) ? "#888888" : colorBox.Text.Trim();
        draft.Kind = (TransactionType)kindBox.SelectedItem;

        Vm.Save(draft);
    }
}
