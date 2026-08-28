using BudgetBook.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace BudgetBook.Controls.Common;

public sealed partial class TransactionListItem : UserControl
{
    public static readonly DependencyProperty TransactionProperty = DependencyProperty.Register(
        nameof(Transaction), typeof(TransactionRecord), typeof(TransactionListItem), new PropertyMetadata(null, OnTransactionChanged));

    public TransactionRecord? Transaction
    {
        get => (TransactionRecord?)GetValue(TransactionProperty);
        set => SetValue(TransactionProperty, value);
    }

    public TransactionListItem()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateTransaction();
    }

    private static void OnTransactionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TransactionListItem)d).UpdateTransaction();

    private void UpdateTransaction()
    {
        if (Transaction is null || DateText is null) return;
        DateText.Text = Transaction.DateLabel;
        CategoryText.Text = Transaction.CategoryName;
        NoteText.Text = Transaction.Note;
        NoteText.Visibility = string.IsNullOrWhiteSpace(Transaction.Note) ? Visibility.Collapsed : Visibility.Visible;
        AccountText.Text = Transaction.AccountName;
        TransactionAmount.Amount = Transaction.SignedAmount;
        TransactionAmount.CurrencyCode = Transaction.Account?.Currency ?? string.Empty;
        TransactionAmount.SetTransactionType(Transaction.Type);
        AutomationProperties.SetName(this, $"{Transaction.CategoryName}, {Transaction.DateLabel}, {Transaction.SignedAmountLabel}");
    }
}
