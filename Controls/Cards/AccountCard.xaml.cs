using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BudgetBook.Controls.Cards;

public sealed partial class AccountCard : UserControl
{
    public static readonly DependencyProperty AccountNameProperty = DependencyProperty.Register(nameof(AccountName), typeof(string), typeof(AccountCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty BalanceLabelProperty = DependencyProperty.Register(nameof(BalanceLabel), typeof(string), typeof(AccountCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty CurrencyCodeProperty = DependencyProperty.Register(nameof(CurrencyCode), typeof(string), typeof(AccountCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(AccountCard), new PropertyMetadata(string.Empty, OnSubtitleChanged));
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(AccountCard), new PropertyMetadata("\uE8C7"));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(AccountCard), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(AccountCard), new PropertyMetadata(null));

    public string AccountName { get => (string)GetValue(AccountNameProperty); set => SetValue(AccountNameProperty, value); }
    public string BalanceLabel { get => (string)GetValue(BalanceLabelProperty); set => SetValue(BalanceLabelProperty, value); }
    public string CurrencyCode { get => (string)GetValue(CurrencyCodeProperty); set => SetValue(CurrencyCodeProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public AccountCard()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateSubtitle();
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((AccountCard)d).UpdateSubtitle();
    private void UpdateSubtitle()
    {
        if (SubtitleText is not null)
            SubtitleText.Visibility = string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
    }
}
