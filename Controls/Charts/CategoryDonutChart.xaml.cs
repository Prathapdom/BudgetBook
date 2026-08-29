using System.Collections;
using System.Collections.Specialized;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using BudgetBook.Reports;

namespace BudgetBook.Controls.Charts;

public sealed record CategoryChartItem(int? CategoryId, string Label, string Value, Microsoft.UI.Xaml.Media.Brush Brush);
public sealed record CategorySelectedEventArgs(int? CategoryId, string Name);

public sealed partial class CategoryDonutChart : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(CategoryDonutChart),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public event EventHandler<CategorySelectedEventArgs>? CategorySelected;
    private INotifyCollectionChanged? _observableSource;

    private IReadOnlyList<CategoryBreakdown> Items
        => ItemsSource?.Cast<object>().OfType<CategoryBreakdown>().ToList() ?? [];

    public CategoryDonutChart()
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
        var summary = new List<CategoryChartItem>();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var color = ChartTheme.Parse(item.ColorHex, ChartTheme.Palette(index));
            var slice = new PieSeries<double>
            {
                Name = item.Name,
                Values = [(double)item.Amount],
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.Transparent),
                InnerRadius = 58,
                HoverPushout = 8,
                AnimationsSpeed = ChartTheme.AnimationSpeed,
                ToolTipLabelFormatter = point => $"{item.Name}: {point.Model:C} ({item.Percentage:P0})",
            };
            slice.DataPointerDown += (_, _) => Select(item);
            series.Add(slice);
            summary.Add(new CategoryChartItem(item.CategoryId, item.Name,
                $"{item.Amount:C} · {item.Percentage:P0}", ChartTheme.Brush(color)));
        }

        Chart.AnimationsSpeed = ChartTheme.AnimationSpeed;
        Chart.TooltipTextPaint = new SolidColorPaint(ChartTheme.Text(ActualTheme));
        Chart.TooltipBackgroundPaint = new SolidColorPaint(ChartTheme.TooltipBackground(ActualTheme));
        Chart.Series = series;
        SummaryList.ItemsSource = summary;
    }

    private void LegendItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CategoryChartItem item })
            CategorySelected?.Invoke(this, new CategorySelectedEventArgs(item.CategoryId, item.Label));
    }

    private void Select(CategoryBreakdown item)
        => CategorySelected?.Invoke(this, new CategorySelectedEventArgs(item.CategoryId, item.Name));

    private static void OnItemsSourceChanged(DependencyObject control, DependencyPropertyChangedEventArgs args)
    {
        var chart = (CategoryDonutChart)control;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged -= chart.Source_CollectionChanged;
        chart._observableSource = args.NewValue as INotifyCollectionChanged;
        if (chart._observableSource is not null) chart._observableSource.CollectionChanged += chart.Source_CollectionChanged;
        chart.Rebuild();
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
}
