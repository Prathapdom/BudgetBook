using System.Globalization;
using BudgetBook.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BudgetBook.Controls.Common;

public enum AmountTone
{
    Automatic,
    Neutral,
    Positive,
    Negative,
    Accent,
}

public sealed partial class AmountText : UserControl
{
    public static readonly DependencyProperty AmountProperty = DependencyProperty.Register(
        nameof(Amount), typeof(decimal), typeof(AmountText), new PropertyMetadata(0m, OnDisplayChanged));
    public static readonly DependencyProperty CurrencyCodeProperty = DependencyProperty.Register(
        nameof(CurrencyCode), typeof(string), typeof(AmountText), new PropertyMetadata(string.Empty, OnDisplayChanged));
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
        nameof(Tone), typeof(AmountTone), typeof(AmountText), new PropertyMetadata(AmountTone.Automatic, OnDisplayChanged));
    public static readonly DependencyProperty ShowSignProperty = DependencyProperty.Register(
        nameof(ShowSign), typeof(bool), typeof(AmountText), new PropertyMetadata(false, OnDisplayChanged));

    public decimal Amount { get => (decimal)GetValue(AmountProperty); set => SetValue(AmountProperty, value); }
    public string CurrencyCode { get => (string)GetValue(CurrencyCodeProperty); set => SetValue(CurrencyCodeProperty, value); }
    public AmountTone Tone { get => (AmountTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public bool ShowSign { get => (bool)GetValue(ShowSignProperty); set => SetValue(ShowSignProperty, value); }

    public AmountText()
    {
        InitializeComponent();
        ActualThemeChanged += (_, _) => UpdateDisplay();
        Loaded += (_, _) => UpdateDisplay();
    }

    public void SetTransactionType(TransactionType type)
    {
        Tone = type == TransactionType.Income ? AmountTone.Positive : AmountTone.Negative;
    }

    private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((AmountText)d).UpdateDisplay();

    private void UpdateDisplay()
    {
        var absoluteAmount = Math.Abs(Amount);
        var formatted = string.IsNullOrWhiteSpace(CurrencyCode)
            ? absoluteAmount.ToString("C", CultureInfo.CurrentCulture)
            : $"{CurrencyCode.ToUpperInvariant()} {absoluteAmount:N2}";
        var sign = ShowSign ? Amount switch { > 0 => "+", < 0 => "−", _ => string.Empty } : string.Empty;
        if (DisplayText is null) return;
        DisplayText.Text = sign + formatted;

        var effectiveTone = Tone == AmountTone.Automatic
            ? Amount switch { > 0 => AmountTone.Positive, < 0 => AmountTone.Negative, _ => AmountTone.Neutral }
            : Tone;
        var key = effectiveTone switch
        {
            AmountTone.Positive => "AppPositiveAmountBrush",
            AmountTone.Negative => "AppNegativeAmountBrush",
            AmountTone.Accent => "AppAccentBrush",
            _ => "AppNeutralAmountBrush",
        };
        if (Application.Current.Resources.TryGetValue(key, out var resource) && resource is Brush brush)
            DisplayText.Foreground = brush;
    }
}
