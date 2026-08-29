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

public sealed record AccountChartItem(string Label, string Value, Microsoft.UI.Xaml.Media.Brush Brush);

public sealed partial class AccountDistributionChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(AccountDistributionChart),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    private IReadOnlyList<AccountBalanceSummary> Items
        => ItemsSource?.Cast<object>().OfType<AccountBalanceSummary>().Where(account => account.Balance > 0m).ToList() ?? [];
    private INotifyCollectionChanged? _observableSource;

    public AccountDistributionChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        ActualThemeChanged += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        if (Chart is null) return;
        var items = Items;
        ContentRoot.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        EmptyRoot.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (items.Count == 0)
        {
            Chart.Series = [];
            SummaryList.ItemsSource = null;
            return;
        }

        var series = new List<ISeries>();
        var summary = new List<AccountChartItem>();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var color = ChartTheme.Palette(index);
            series.Add(new PieSeries<double>
            {
                Name = item.Name,
                Values = [(double)item.Balance],
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.Transparent),
                InnerRadius = 58,
                HoverPushout = 7,
                AnimationsSpeed = ChartTheme.AnimationSpeed,
                ToolTipLabelFormatter = point => $"{item.Name}: {point.Model:C} {item.Currency}",
            });
            summary.Add(new AccountChartItem(item.Name,
                $"{item.Balance:C} {item.Currency} · {item.PercentageOfCurrencyTotal:P0}",
                ChartTheme.Brush(color)));
        }

        Chart.Series = series;
        Chart.AnimationsSpeed = ChartTheme.AnimationSpeed;
        Chart.TooltipTextPaint = new SolidColorPaint(ChartTheme.Text(ActualTheme));
        Chart.TooltipBackgroundPaint = new SolidColorPaint(ChartTheme.TooltipBackground(ActualTheme));
        SummaryList.ItemsSource = summary;
    }

    private static void OnItemsSourceChanged(DependencyObject control, DependencyPropertyChangedEventArgs args)
    {
        var chart = (AccountDistributionChart)control;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged -= chart.Source_CollectionChanged;
        chart._observableSource = args.NewValue as INotifyCollectionChanged;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged += chart.Source_CollectionChanged;
        chart.Rebuild();
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
}
