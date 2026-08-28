using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BudgetBook.Controls.Common;

public sealed partial class EmptyState : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata("Nothing here yet"));
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(EmptyState), new PropertyMetadata("\uE946"));
    public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty, OnActionChanged));
    public static readonly DependencyProperty ActionCommandProperty = DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState), new PropertyMetadata(null, OnActionChanged));
    public static readonly DependencyProperty ActionCommandParameterProperty = DependencyProperty.Register(nameof(ActionCommandParameter), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public object? ActionCommandParameter { get => GetValue(ActionCommandParameterProperty); set => SetValue(ActionCommandParameterProperty, value); }

    public EmptyState()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateActionVisibility();
    }

    private static void OnActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EmptyState)d).UpdateActionVisibility();
    private void UpdateActionVisibility()
    {
        if (ActionButton is not null)
            ActionButton.Visibility = string.IsNullOrWhiteSpace(ActionText) || ActionCommand is null ? Visibility.Collapsed : Visibility.Visible;
    }
}
