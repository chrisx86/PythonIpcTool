using System.Windows;
using System.Windows.Controls;

namespace PythonIpcTool.Extensions;

/// <summary>
/// Provides an attached property to enable auto-scrolling to the end for a TextBox.
/// When the text content changes, it automatically scrolls the view to the last line.
/// </summary>
public static class TextBoxExtensions
{
    // Define the attached property
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(TextBoxExtensions),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    // Getter
    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    // Setter
    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    /// <summary>
    /// Callback method executed when the AutoScrollToEnd property is set.
    /// </summary>
    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // The property can only be attached to a TextBox.
        if (d is not TextBox textBox)
        {
            return;
        }

        // The event handler that will perform the scroll
        var scrollHandler = new TextChangedEventHandler(
            (sender, args) =>
            {
                // The ScrollToEnd() method is built into the TextBox control.
                textBox.ScrollToEnd();
            });

        if ((bool)e.NewValue)
        {
            // If AutoScrollToEnd is set to true, subscribe to the TextChanged event.
            textBox.TextChanged += scrollHandler;
            // Also, scroll to end immediately in case there's already text.
            textBox.ScrollToEnd();
        }
        else
        {
            // If AutoScrollToEnd is set to false, unsubscribe to prevent memory leaks.
            textBox.TextChanged -= scrollHandler;
        }
    }
}