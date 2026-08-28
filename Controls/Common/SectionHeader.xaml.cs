using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BudgetBook.Controls.Common;

public sealed partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty, OnContentChanged));
    public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty, OnContentChanged));
    public static readonly DependencyProperty ActionCommandProperty = DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(SectionHeader), new PropertyMetadata(null, OnContentChanged));
    public static readonly DependencyProperty ActionCommandParameterProperty = DependencyProperty.Register(nameof(ActionCommandParameter), typeof(object), typeof(SectionHeader), new PropertyMetadata(null));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string ActionText { get => (string)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public object? ActionCommandParameter { get => GetValue(ActionCommandParameterProperty); set => SetValue(ActionCommandParameterProperty, value); }

    public SectionHeader()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateVisibility();
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SectionHeader)d).UpdateVisibility();
    private void UpdateVisibility()
    {
        if (SubtitleText is not null)
            SubtitleText.Visibility = string.IsNullOrWhiteSpace(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        if (ActionButton is not null)
            ActionButton.Visibility = string.IsNullOrWhiteSpace(ActionText) || ActionCommand is null ? Visibility.Collapsed : Visibility.Visible;
    }
}
