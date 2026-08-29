using System.Collections;
using System.Collections.Specialized;
using BudgetBook.Reports;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace BudgetBook.Controls.Charts;

public sealed partial class IncomeExpenseChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(IncomeExpenseChart),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    private IReadOnlyList<IncomeExpensePoint> Items
        => ItemsSource?.Cast<object>().OfType<IncomeExpensePoint>().ToList() ?? [];
    private INotifyCollectionChanged? _observableSource;

    public IncomeExpenseChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        ActualThemeChanged += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        if (Chart is null) return;
        var items = Items;
        var hasValues = items.Any(point => point.Income != 0m || point.Expense != 0m);
        ContentRoot.Visibility = hasValues ? Visibility.Visible : Visibility.Collapsed;
        EmptyRoot.Visibility = hasValues ? Visibility.Collapsed : Visibility.Visible;
        if (!hasValues)
        {
            Chart.Series = [];
            SummaryList.ItemsSource = null;
            return;
        }

        var positive = ChartTheme.Color("AppPositiveAmountBrush", SKColors.SeaGreen);
        var negative = ChartTheme.Color("AppNegativeAmountBrush", SKColors.IndianRed);
        var text = ChartTheme.Text(ActualTheme);
        var grid = ChartTheme.Grid(ActualTheme);
        Chart.Series =
        [
            new ColumnSeries<double>
            {
                Name = "Income",
                Values = items.Select(point => (double)point.Income).ToArray(),
                Fill = new SolidColorPaint(positive),
                Stroke = null,
                Rx = 5,
                Ry = 5,
                AnimationsSpeed = ChartTheme.AnimationSpeed,
                YToolTipLabelFormatter = point => $"Income: {point.Model:C}",
            },
            new ColumnSeries<double>
            {
                Name = "Expenses",
                Values = items.Select(point => (double)point.Expense).ToArray(),
                Fill = new SolidColorPaint(negative),
                Stroke = new SolidColorPaint(negative.WithAlpha(190), 2),
                Rx = 1,
                Ry = 1,
                AnimationsSpeed = ChartTheme.AnimationSpeed,
                YToolTipLabelFormatter = point => $"Expenses: {point.Model:C}",
            },
        ];
        Chart.XAxes = [new Axis { Labels = items.Select(FormatDate).ToArray(), LabelsPaint = new SolidColorPaint(text), SeparatorsPaint = null }];
        Chart.YAxes = [new Axis { Labeler = value => value.ToString("C0"), LabelsPaint = new SolidColorPaint(text), SeparatorsPaint = new SolidColorPaint(grid) }];
        Chart.LegendTextPaint = new SolidColorPaint(text);
        Chart.TooltipTextPaint = new SolidColorPaint(text);
        Chart.TooltipBackgroundPaint = new SolidColorPaint(ChartTheme.TooltipBackground(ActualTheme));
        Chart.AnimationsSpeed = ChartTheme.AnimationSpeed;
        SummaryList.ItemsSource = items.Select(point => new ChartSummaryRow(
            FormatDate(point), $"Income {point.Income:C}", $"Expenses {point.Expense:C}")).ToList();
    }

    private static string FormatDate(IncomeExpensePoint point)
        => point.PeriodStart.ToString("MMM d");

    private static void OnItemsSourceChanged(DependencyObject control, DependencyPropertyChangedEventArgs args)
    {
        var chart = (IncomeExpenseChart)control;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged -= chart.Source_CollectionChanged;
        chart._observableSource = args.NewValue as INotifyCollectionChanged;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged += chart.Source_CollectionChanged;
        chart.Rebuild();
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
}
