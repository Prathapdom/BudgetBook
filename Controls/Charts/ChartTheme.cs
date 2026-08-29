using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.UI.ViewManagement;

namespace BudgetBook.Controls.Charts;

internal static class ChartTheme
{
    public static TimeSpan AnimationSpeed
    {
        get
        {
            try
            {
                return new UISettings().AnimationsEnabled ? TimeSpan.FromMilliseconds(220) : TimeSpan.Zero;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
    }

    public static SKColor Color(string resourceKey, SKColor fallback)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var value)
            && value is SolidColorBrush brush)
        {
            var color = brush.Color;
            return new SKColor(color.R, color.G, color.B, color.A);
        }

        return fallback;
    }

    public static SKColor Parse(string? colorHex, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(colorHex)) return fallback;
        try { return SKColor.Parse(colorHex); }
        catch { return fallback; }
    }

    public static SKColor Palette(int index)
        => Color($"AppChartPalette{index % 8 + 1}", DefaultPalette[index % DefaultPalette.Length]);

    public static SolidColorPaint Paint(string resourceKey, SKColor fallback, float thickness = 1)
        => new(Color(resourceKey, fallback), thickness);

    public static SolidColorBrush Brush(SKColor color)
        => new(Windows.UI.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));

    public static SKColor Text(ElementTheme theme)
        => theme == ElementTheme.Dark ? new SKColor(245, 245, 245) : new SKColor(48, 48, 48);

    public static SKColor Grid(ElementTheme theme)
        => theme == ElementTheme.Dark ? new SKColor(255, 255, 255, 46) : new SKColor(0, 0, 0, 40);

    public static SKColor TooltipBackground(ElementTheme theme)
        => theme == ElementTheme.Dark ? new SKColor(32, 32, 32) : new SKColor(250, 250, 250);

    private static readonly SKColor[] DefaultPalette =
    [
        SKColors.DodgerBlue,
        SKColors.SeaGreen,
        SKColors.MediumPurple,
        SKColors.Tomato,
        SKColors.DarkOrange,
        SKColors.Teal,
        SKColors.Firebrick,
        SKColors.SlateBlue,
    ];
}
