using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace BudgetBook.Controls.Cards;

public sealed class AppCard : ContentControl
{
    private Border? _cardBorder;
    private ContentPresenter? _headerPresenter;
    private ContentPresenter? _footerPresenter;

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(object), typeof(AppCard), new PropertyMetadata(null, OnSlotChanged));

    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
        nameof(Footer), typeof(object), typeof(AppCard), new PropertyMetadata(null, OnSlotChanged));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(AppCard), new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(AppCard), new PropertyMetadata(null));

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public AppCard()
    {
        IsTabStop = false;
        Tapped += OnTapped;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
        ActualThemeChanged += (_, _) => ResetBackground();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _cardBorder = GetTemplateChild("CardBorder") as Border;
        _headerPresenter = GetTemplateChild("HeaderPresenter") as ContentPresenter;
        _footerPresenter = GetTemplateChild("FooterPresenter") as ContentPresenter;
        UpdateSlotVisibility();
        ResetBackground();
    }

    private static void OnSlotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AppCard)d).UpdateSlotVisibility();

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AppCard)d).IsTabStop = e.NewValue is ICommand;

    private void UpdateSlotVisibility()
    {
        if (_headerPresenter is not null)
            _headerPresenter.Visibility = Header is null ? Visibility.Collapsed : Visibility.Visible;
        if (_footerPresenter is not null)
            _footerPresenter.Visibility = Footer is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!CanExecute()) return;
        Command!.Execute(CommandParameter);
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not (VirtualKey.Enter or VirtualKey.Space) || !CanExecute()) return;
        Command!.Execute(CommandParameter);
        e.Handled = true;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        => SetBackground("AppCardBackgroundHoverBrush");

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        Opacity = 1;
        ResetBackground();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Command is not null) Opacity = 0.84;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e) => Opacity = 1;

    private bool CanExecute() => Command?.CanExecute(CommandParameter) == true;

    private void ResetBackground() => SetBackground("AppCardBackgroundBrush");

    private void SetBackground(string resourceKey)
    {
        if (_cardBorder is not null && Resources.TryGetValue(resourceKey, out var local) && local is Brush localBrush)
        {
            _cardBorder.Background = localBrush;
            return;
        }

        if (_cardBorder is not null && Application.Current.Resources.TryGetValue(resourceKey, out var app) && app is Brush appBrush)
            _cardBorder.Background = appBrush;
    }
}
