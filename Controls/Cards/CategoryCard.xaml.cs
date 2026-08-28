using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BudgetBook.Controls.Cards;

public sealed partial class CategoryCard : UserControl
{
    public static readonly DependencyProperty CategoryNameProperty = DependencyProperty.Register(nameof(CategoryName), typeof(string), typeof(CategoryCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty KindLabelProperty = DependencyProperty.Register(nameof(KindLabel), typeof(string), typeof(CategoryCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(CategoryCard), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(CategoryCard), new PropertyMetadata("\uE8EC"));
    public static readonly DependencyProperty CategoryBrushProperty = DependencyProperty.Register(nameof(CategoryBrush), typeof(Brush), typeof(CategoryCard), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(CategoryCard), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(CategoryCard), new PropertyMetadata(null));

    public string CategoryName { get => (string)GetValue(CategoryNameProperty); set => SetValue(CategoryNameProperty, value); }
    public string KindLabel { get => (string)GetValue(KindLabelProperty); set => SetValue(KindLabelProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public Brush? CategoryBrush { get => (Brush?)GetValue(CategoryBrushProperty); set => SetValue(CategoryBrushProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public CategoryCard() => InitializeComponent();
}
