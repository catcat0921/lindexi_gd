using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace FenallqearqaCewuwhallferbo;

[ContentProperty(nameof(Layout))]
public sealed class LayoutContentControl : ContentControl
{
    public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
        nameof(Layout),
        typeof(UIElement),
        typeof(LayoutContentControl),
        new PropertyMetadata(null, OnLayoutChanged));

    public UIElement? Layout
    {
        get => (UIElement?)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    private static void OnLayoutChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var control = (LayoutContentControl)dependencyObject;
        control.Content = eventArgs.NewValue;
    }
}
