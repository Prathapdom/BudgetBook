using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BudgetBook.Controls.Cards;

public enum BudgetProgressStatus
{
    Normal,
    Warning,
    Critical,
    Exceeded,
}

public sealed partial class BudgetProgressCard : UserControl
{
    public static readonly DependencyProperty CategoryNameProperty = DependencyProperty.Register(nameof(CategoryName), typeof(string), typeof(BudgetProgressCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SpentLabelProperty = DependencyProperty.Register(nameof(SpentLabel), typeof(string), typeof(BudgetProgressCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty LimitLabelProperty = DependencyProperty.Register(nameof(LimitLabel), typeof(string), typeof(BudgetProgressCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty RemainingLabelProperty = DependencyProperty.Register(nameof(RemainingLabel), typeof(string), typeof(BudgetProgressCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(double), typeof(BudgetProgressCard), new PropertyMetadata(0d));
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(nameof(Status), typeof(BudgetProgressStatus), typeof(BudgetProgressCard), new PropertyMetadata(BudgetProgressStatus.Normal, OnStatusChanged));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BudgetProgressCard), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BudgetProgressCard), new PropertyMetadata(null));

    public string CategoryName { get => (string)GetValue(CategoryNameProperty); set => SetValue(CategoryNameProperty, value); }
    public string SpentLabel { get => (string)GetValue(SpentLabelProperty); set => SetValue(SpentLabelProperty, value); }
    public string LimitLabel { get => (string)GetValue(LimitLabelProperty); set => SetValue(LimitLabelProperty, value); }
    public string RemainingLabel { get => (string)GetValue(RemainingLabelProperty); set => SetValue(RemainingLabelProperty, value); }
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public BudgetProgressStatus Status { get => (BudgetProgressStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public BudgetProgressCard()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateStatus();
        ActualThemeChanged += (_, _) => UpdateStatus();
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BudgetProgressCard)d).UpdateStatus();

    private void UpdateStatus()
    {
        if (BudgetProgress is null) return;
        var key = Status switch
        {
            BudgetProgressStatus.Warning => "AppChartPalette5",
            BudgetProgressStatus.Critical => "AppChartPalette4",
            BudgetProgressStatus.Exceeded => "AppNegativeAmountBrush",
            _ => "AppPositiveAmountBrush",
        };
        if (Application.Current.Resources.TryGetValue(key, out var resource) && resource is Brush brush)
        {
            BudgetProgress.Foreground = brush;
            RemainingText.Foreground = brush;
        }
    }
}
