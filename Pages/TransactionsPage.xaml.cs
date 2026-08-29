using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using Windows.Foundation;
using BudgetBook.Data;
using BudgetBook.ViewModels;
using BudgetBook.Services.Interfaces;
using Windows.Storage.Pickers;

namespace BudgetBook.Pages;

public sealed partial class TransactionsPage : Page
{
    public TransactionsViewModel Vm { get; }
    public Array TransactionTypes { get; } = Enum.GetValues(typeof(TransactionType));
    private bool _openNewTransactionRequested;
    private bool _dataReady;
    private TransactionRecord? _requestedDraft;
    private TransactionRecord? _undoTransaction;
    private readonly IAttachmentService _attachments;

    public TransactionsPage()
    {
        Vm = App.GetService<TransactionsViewModel>();
        _attachments = App.GetService<IAttachmentService>();
        InitializeComponent();
        GroupedTransactions.Source = Vm.Groups;
        Loaded += (_, _) => TryOpenRequestedTransaction();
        SizeChanged += (_, _) => UpdateEditorLayout();
        AddShortcut(VirtualKey.N, VirtualKeyModifiers.Control, (_, _) => _ = ShowEditorAsync(Vm.NewDraft(), true));
        AddShortcut(VirtualKey.F, VirtualKeyModifiers.Control, (_, args) => { SearchBox.Focus(FocusState.Keyboard); args.Handled = true; });
        AddShortcut(VirtualKey.Delete, VirtualKeyModifiers.None, (_, _) => _ = DeleteSelectedAsync());
    }

    private void AddShortcut(VirtualKey key, VirtualKeyModifiers modifiers, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += handler;
        KeyboardAccelerators.Add(accelerator);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var request = e.Parameter as TransactionsNavigationRequest;
        if (request is not null) Vm.ApplyNavigationRequest(request);
        _openNewTransactionRequested = request?.OpenNewTransaction == true;
        _requestedDraft = request?.Draft;
        _dataReady = false;
        await Vm.RefreshAsync();
        AccountFilterBox.SelectedItem = Vm.AccountFilters.FirstOrDefault(option => option.Id == Vm.AccountFilterId) ?? Vm.AccountFilters.FirstOrDefault();
        CategoryFilterBox.SelectedItem = Vm.CategoryFilters.FirstOrDefault(option => option.Id == Vm.CategoryFilterId) ?? Vm.CategoryFilters.FirstOrDefault();
        _dataReady = true;
        TryOpenRequestedTransaction();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) { _dataReady = false; base.OnNavigatedFrom(e); }

    private void TryOpenRequestedTransaction()
    {
        if (!_openNewTransactionRequested || !_dataReady || XamlRoot is null) return;
        _openNewTransactionRequested = false;
        var draft = _requestedDraft ?? Vm.NewDraft();
        _requestedDraft = null;
        if (draft.AccountId == 0) draft.AccountId = Vm.Accounts.FirstOrDefault()?.Id ?? 0;
        _ = ShowEditorAsync(draft, true);
    }

    private async void Add_Click(object sender, RoutedEventArgs e) => await ShowEditorAsync(Vm.NewDraft(), true);

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is null) return;
        if (ActualWidth >= 1000) OpenEditorPane(Vm.Selected); else await ShowEditorAsync(Clone(Vm.Selected), false);
    }

    private async void List_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (Vm.Selected is null) return;
        if (ActualWidth >= 1000) OpenEditorPane(Vm.Selected); else await ShowEditorAsync(Clone(Vm.Selected), false);
    }

    private void TransactionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActualWidth >= 1000 && Vm.Selected is not null) OpenEditorPane(Vm.Selected);
    }

    private static TransactionRecord Clone(TransactionRecord transaction) => new()
    {
        Id = transaction.Id, Amount = transaction.Amount, Date = transaction.Date, Type = transaction.Type,
        Note = transaction.Note, AccountId = transaction.AccountId, CategoryId = transaction.CategoryId,
        Tags = transaction.Tags, TransferGroupId = transaction.TransferGroupId, TransferDirection = transaction.TransferDirection,
    };

    private void OpenEditorPane(TransactionRecord transaction)
    {
        var draft = Clone(transaction);
        PaneTypeBox.SelectedItem = draft.Type;
        PaneAmountBox.Value = (double)draft.Amount;
        PaneDatePicker.Date = new DateTimeOffset(draft.Date);
        PaneAccountBox.SelectedItem = Vm.Accounts.FirstOrDefault(account => account.Id == draft.AccountId);
        PaneCategoryBox.SelectedItem = Vm.Categories.FirstOrDefault(category => category.Id == draft.CategoryId);
        PaneNoteBox.Text = draft.Note;
        PaneTagsBox.Text = draft.Tags;
        EditorPane.Tag = draft;
        EditorPane.Visibility = Visibility.Visible;
        UpdateEditorLayout();
    }

    private void UpdateEditorLayout()
    {
        if (EditorPane is null || EditorColumn is null) return;
        if (ActualWidth < 1000) { EditorPane.Visibility = Visibility.Collapsed; EditorColumn.Width = new GridLength(0); }
        else EditorColumn.Width = EditorPane.Visibility == Visibility.Visible ? new GridLength(360) : new GridLength(0);
    }

    private void CloseEditor_Click(object sender, RoutedEventArgs e) { EditorPane.Visibility = Visibility.Collapsed; UpdateEditorLayout(); }

    private async void SavePane_Click(object sender, RoutedEventArgs e)
    {
        if (EditorPane.Tag is not TransactionRecord draft || !ReadEditor(draft, PaneTypeBox, PaneAmountBox, PaneDatePicker, PaneAccountBox, PaneCategoryBox, PaneNoteBox, PaneTagsBox)) return;
        await Vm.SaveAsync(draft);
        EditorPane.Visibility = Visibility.Collapsed;
        UpdateEditorLayout();
    }

    private async System.Threading.Tasks.Task ShowEditorAsync(TransactionRecord draft, bool isNew)
    {
        var typeBox = new ComboBox { Header = "Type", ItemsSource = TransactionTypes, SelectedItem = draft.Type, HorizontalAlignment = HorizontalAlignment.Stretch };
        var amountBox = new NumberBox { Header = "Amount", Value = (double)draft.Amount, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Minimum = 0 };
        var datePicker = new DatePicker { Header = "Date", Date = new DateTimeOffset(draft.Date) };
        var accountBox = new ComboBox { Header = "Account", ItemsSource = Vm.Accounts, DisplayMemberPath = nameof(Account.Name), SelectedItem = Vm.Accounts.FirstOrDefault(a => a.Id == draft.AccountId) ?? Vm.Accounts.FirstOrDefault(), HorizontalAlignment = HorizontalAlignment.Stretch };
        var categoryBox = new ComboBox { Header = "Category", ItemsSource = Vm.Categories, DisplayMemberPath = nameof(Category.Name), SelectedItem = Vm.Categories.FirstOrDefault(c => c.Id == draft.CategoryId), HorizontalAlignment = HorizontalAlignment.Stretch };
        var destinationBox = new ComboBox { Header = "To account (transfers)", ItemsSource = Vm.Accounts, DisplayMemberPath = nameof(Account.Name), HorizontalAlignment = HorizontalAlignment.Stretch, Visibility = draft.Type == TransactionType.Transfer ? Visibility.Visible : Visibility.Collapsed };
        var noteBox = new TextBox { Header = "Note", Text = draft.Note };
        var tagsBox = new TextBox { Header = "Tags", Text = draft.Tags, PlaceholderText = "travel, reimbursable" };
        typeBox.SelectionChanged += (_, _) =>
        {
            var transfer = typeBox.SelectedItem is TransactionType.Transfer;
            destinationBox.Visibility = transfer ? Visibility.Visible : Visibility.Collapsed;
            categoryBox.Visibility = transfer ? Visibility.Collapsed : Visibility.Visible;
        };
        var panel = new StackPanel { Spacing = 12, MinWidth = 320 };
        foreach (var control in new Control[] { typeBox, amountBox, datePicker, accountBox, destinationBox, categoryBox, noteBox, tagsBox }) panel.Children.Add(control);
        var dialog = new ContentDialog { Title = isNew ? "New transaction" : "Edit transaction", PrimaryButtonText = "Save", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary, Content = panel, XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || !ReadEditor(draft, typeBox, amountBox, datePicker, accountBox, categoryBox, noteBox, tagsBox)) return;
        if (isNew && draft.Type == TransactionType.Transfer)
        {
            if (destinationBox.SelectedItem is not Account destination || destination.Id == draft.AccountId) return;
            await App.GetService<ITransactionService>().SaveTransferAsync(draft.AccountId, destination.Id, draft.Amount, draft.Date, draft.Note, draft.Tags);
            await Vm.RefreshAsync();
        }
        else await Vm.SaveAsync(draft);
    }

    private bool ReadEditor(TransactionRecord draft, ComboBox typeBox, NumberBox amountBox, DatePicker datePicker, ComboBox accountBox, ComboBox categoryBox, TextBox noteBox, TextBox tagsBox)
    {
        draft.Type = typeBox.SelectedItem is TransactionType type ? type : TransactionType.Expense;
        draft.Amount = (decimal)(double.IsNaN(amountBox.Value) ? 0 : amountBox.Value);
        draft.Date = datePicker.Date.DateTime.Date;
        draft.AccountId = (accountBox.SelectedItem as Account)?.Id ?? 0;
        draft.CategoryId = (categoryBox.SelectedItem as Category)?.Id;
        draft.Note = noteBox.Text?.Trim() ?? string.Empty;
        draft.Tags = tagsBox.Text?.Trim() ?? string.Empty;
        return draft.AccountId != 0 && draft.Amount > 0;
    }

    private async void Attach_Click(object sender, RoutedEventArgs e)
    {
        if (Vm.Selected is not { Id: > 0 } transaction) return;
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".jpg"); picker.FileTypeFilter.Add(".jpeg"); picker.FileTypeFilter.Add(".png"); picker.FileTypeFilter.Add(".pdf");
        if (MainWindow.Instance is not null) WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance));
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        var attachment = await _attachments.AddAttachmentAsync(transaction.Id, file.Path);
        await new ContentDialog { Title = "Receipt attached", Content = attachment.FileName, CloseButtonText = "OK", XamlRoot = XamlRoot }.ShowAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e) => await DeleteSelectedAsync();

    private async System.Threading.Tasks.Task DeleteSelectedAsync()
    {
        if (Vm.Selected is not { } selected) return;
        var result = await new ContentDialog { Title = "Delete transaction?", Content = $"Delete {selected.SignedAmountLabel} for {selected.CategoryName}?", PrimaryButtonText = "Delete", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close, XamlRoot = XamlRoot }.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        _undoTransaction = Clone(selected);
        await Vm.DeleteAsync(selected);
        UndoBar.IsOpen = true;
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undoTransaction is null) return;
        var transaction = _undoTransaction;
        _undoTransaction = null;
        await Vm.RestoreAsync(transaction);
        UndoBar.IsOpen = false;
    }

    private void TypeFilter_Changed(object sender, SelectionChangedEventArgs e) => Vm.TypeFilter = TypeFilterBox.SelectedIndex switch { 1 => TransactionType.Expense, 2 => TransactionType.Income, 3 => TransactionType.Transfer, _ => null };
    private void AccountFilter_Changed(object sender, SelectionChangedEventArgs e) { if (AccountFilterBox.SelectedItem is TransactionFilterOption option) Vm.AccountFilterId = option.Id; }
    private void CategoryFilter_Changed(object sender, SelectionChangedEventArgs e) { if (CategoryFilterBox.SelectedItem is TransactionFilterOption option) { Vm.CategoryFilterId = option.Id; Vm.UncategorizedOnly = false; } }
}
