using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace PythonIpcTool.Extensions;

/// <summary>
/// Provides an attached property to enable robust auto-scrolling for any ItemsControl.
/// When new items are added, it reliably scrolls to the last item by deferring the scroll action
/// until after the UI has finished its layout and rendering pass.
/// </summary>
public static class ScrollViewerExtensions
{
    // 1. Define the Attached Property
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(ScrollViewerExtensions),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    // 2. Standard Get/Set methods for the Attached Property
    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    // 3. The core logic in the PropertyChanged callback
    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // We are specifically targeting ItemsControl (like ListView)
        if (d is not ItemsControl itemsControl)
        {
            return;
        }

        // We need to subscribe/unsubscribe to the CollectionChanged event of the ItemsSource.
        // The ItemsSource must implement INotifyCollectionChanged for this to work (like ObservableCollection).
        if (itemsControl.ItemsSource is INotifyCollectionChanged collection)
        {
            var scrollHandler = new NotifyCollectionChangedEventHandler(
                (sender, args) =>
                {
                    // Only act when new items are added
                    if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
                    {
                        // --- THIS IS THE FIX ---
                        // Check if the control is a ListBox or ListView, which have ScrollIntoView.
                        // The 'is' pattern matching also declares the variable 'listBox'.
                        if (itemsControl is ListBox listBox)
                        {
                            object? lastItem = args.NewItems[args.NewItems.Count - 1];
                            if (lastItem != null)
                            {
                                listBox.ScrollIntoView(lastItem);
                            }
                        }
                        // You can add a similar check for ListView if needed, but ListBox covers both
                        // since ListView inherits from ListBox.
                    }
                });

            if ((bool)e.NewValue)
            {
                // If AutoScrollToEnd is set to true, subscribe to the event.
                collection.CollectionChanged += scrollHandler;
            }
            else
            {
                // If AutoScrollToEnd is set to false, unsubscribe.
                collection.CollectionChanged -= scrollHandler;
            }
        }
    }
}