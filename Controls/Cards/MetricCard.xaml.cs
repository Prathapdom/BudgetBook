using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BudgetBook.Controls.Cards;

public enum MetricCardTone
{
    Neutral,
    Positive,
    Negative,
    Accent,
}

public sealed partial class MetricCard : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(MetricCard), new PropertyMetadata(string.Empty, OnSubtitleChanged));
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(MetricCard), new PropertyMetadata("\uE8C7", OnGlyphChanged));
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
        nameof(Tone), typeof(MetricCardTone), typeof(MetricCard), new PropertyMetadata(MetricCardTone.Neutral, OnToneChanged));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(MetricCard), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(MetricCard), new PropertyMetadata(null));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public MetricCardTone Tone { get => (MetricCardTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public MetricCard()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateVisuals();
        ActualThemeChanged += (_, _) => UpdateVisuals();
    }

    private static void OnToneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MetricCard)d).UpdateVisuals();
    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MetricCard)d).UpdateVisuals();
    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((MetricCard)d).UpdateVisuals();

    private void UpdateVisuals()
    {
        if (ValueText is null) return;
        var key = Tone switch
        {
            MetricCardTone.Positive => "AppPositiveAmountBrush",
            MetricCardTone.Negative => "AppNegativeAmountBrush",
            MetricCardTone.Accent => "AppAccentBrush",
            _ => "AppNeutralAmountBrush",
        };
        if (Application.Current.Resources.TryGetValue(key, out var resource) && resource is Brush brush)
            ValueText.Foreground = brush;

        SupportingText.Visibility = string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        IconContainer.Visibility = string.IsNullOrWhiteSpace(Glyph) ? Visibility.Collapsed : Visibility.Visible;
    }
}
