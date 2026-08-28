using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BudgetBook;

/// <summary>Hex string ("#RRGGBB" / "#AARRGGBB") -> SolidColorBrush.</summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hex = value as string;
        return new SolidColorBrush(ParseColor(hex));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();

    public static Color ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Colors.Gray;
        hex = hex.TrimStart('#');
        try
        {
            byte a = 255, r, g, b;
            if (hex.Length == 8)
            {
                a = System.Convert.ToByte(hex.Substring(0, 2), 16);
                r = System.Convert.ToByte(hex.Substring(2, 2), 16);
                g = System.Convert.ToByte(hex.Substring(4, 2), 16);
                b = System.Convert.ToByte(hex.Substring(6, 2), 16);
            }
            else
            {
                r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            }
            return Color.FromArgb(a, r, g, b);
        }
        catch
        {
            return Colors.Gray;
        }
    }
}

/// <summary>Fraction (0..1) -> pixel width, scaled by ConverterParameter (default 260).</summary>
public class FractionToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double fraction = value is double d ? d : 0;
        double scale = 260;
        if (parameter is string s && double.TryParse(s, out var parsed)) scale = parsed;
        return Math.Max(2.0, fraction * scale);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>decimal / double -> currency string.</summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value switch
        {
            decimal m => m.ToString("C"),
            double db => db.ToString("C"),
            _ => value?.ToString() ?? "",
        };

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = value is bool v && v;
        if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase)) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility vis && vis == Visibility.Visible;
}
