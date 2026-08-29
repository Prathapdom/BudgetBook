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

public sealed record ChartSummaryRow(string Label, string PrimaryValue, string SecondaryValue = "");

public sealed partial class CashFlowChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(CashFlowChart),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    private IReadOnlyList<CashFlowPoint> Items
        => ItemsSource?.Cast<object>().OfType<CashFlowPoint>().ToList() ?? [];
    private INotifyCollectionChanged? _observableSource;

    public CashFlowChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        ActualThemeChanged += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        if (Chart is null) return;
        var items = Items;
        var hasValues = items.Any(point => point.NetMovement != 0m);
        ContentRoot.Visibility = hasValues ? Visibility.Visible : Visibility.Collapsed;
        EmptyRoot.Visibility = hasValues ? Visibility.Collapsed : Visibility.Visible;
        if (!hasValues)
        {
            Chart.Series = [];
            SummaryList.ItemsSource = null;
            return;
        }

        var accent = ChartTheme.Color("AppChartPalette1", SKColors.DodgerBlue);
        var text = ChartTheme.Text(ActualTheme);
        var grid = ChartTheme.Grid(ActualTheme);
        var labels = items.Select(FormatDate).ToArray();
        Chart.Series =
        [
            new LineSeries<double>
            {
                Name = "Net movement",
                Values = items.Select(point => (double)point.NetMovement).ToArray(),
                Stroke = new SolidColorPaint(accent, 3),
                Fill = new SolidColorPaint(accent.WithAlpha(38)),
                GeometryFill = new SolidColorPaint(accent),
                GeometryStroke = new SolidColorPaint(accent, 2),
                GeometrySize = 7,
                LineSmoothness = 0.35,
                AnimationsSpeed = ChartTheme.AnimationSpeed,
                YToolTipLabelFormatter = point => $"Net: {point.Model:C}",
            },
        ];
        Chart.XAxes = [new Axis { Labels = labels, LabelsPaint = new SolidColorPaint(text), SeparatorsPaint = null }];
        Chart.YAxes = [new Axis { Labeler = value => value.ToString("C0"), LabelsPaint = new SolidColorPaint(text), SeparatorsPaint = new SolidColorPaint(grid) }];
        Chart.TooltipTextPaint = new SolidColorPaint(text);
        Chart.TooltipBackgroundPaint = new SolidColorPaint(ChartTheme.TooltipBackground(ActualTheme));
        Chart.AnimationsSpeed = ChartTheme.AnimationSpeed;
        SummaryList.ItemsSource = items.Select(point => new ChartSummaryRow(
            FormatDate(point), $"Movement {point.NetMovement:C}", $"Running {point.RunningNet:C}")).ToList();
    }

    private static string FormatDate(CashFlowPoint point)
        => point.PeriodStart.ToString("MMM d");

    private static void OnItemsSourceChanged(DependencyObject control, DependencyPropertyChangedEventArgs args)
    {
        var chart = (CashFlowChart)control;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged -= chart.Source_CollectionChanged;
        chart._observableSource = args.NewValue as INotifyCollectionChanged;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged += chart.Source_CollectionChanged;
        chart.Rebuild();
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
}
