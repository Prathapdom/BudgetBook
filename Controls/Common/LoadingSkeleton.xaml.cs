using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BudgetBook.Controls.Common;

public sealed partial class LoadingSkeleton : UserControl
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(LoadingSkeleton), new PropertyMetadata(true, OnIsActiveChanged));

    public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

    public LoadingSkeleton()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateAnimation();
        Unloaded += (_, _) => PulseStoryboard.Stop();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((LoadingSkeleton)d).UpdateAnimation();

    private void UpdateAnimation()
    {
        if (!IsLoaded) return;
        if (IsActive) PulseStoryboard.Begin();
        else PulseStoryboard.Stop();
    }
}
