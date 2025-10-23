using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PythonIpcTool.Extensions;

/// <summary>
/// Provides an attached property to enable robust auto-scrolling for any ItemsControl.
/// When new items are added, it reliably scrolls to the last item by deferring the scroll action
/// until after the UI has finished its layout and rendering pass.
/// </summary>
public static class ScrollViewerExtensions
{
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(ScrollViewerExtensions),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl itemsControl)
        {
            return;
        }

        // The main handler for scrolling when the collection changes
        var scrollHandler = new NotifyCollectionChangedEventHandler(
            (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
                {
                    itemsControl.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            // --- THIS IS THE TYPE-SAFETY FIX ---
                            // 1. We must check if the control is a ListBox (or a derived class like ListView).
                            if (itemsControl is ListBox listBox && listBox.Items.Count > 0)
                            {
                                // 2. Get the last item.
                                object lastItem = listBox.Items[listBox.Items.Count - 1];

                                // 3. Now, call ScrollIntoView on the correctly typed 'listBox' variable.
                                listBox.ScrollIntoView(lastItem);
                            }
                        }),
                        DispatcherPriority.ApplicationIdle
                    );
                }
            });

        // This handler subscribes to events when the control is loaded
        var loadedHandler = new RoutedEventHandler((sender, args) =>
        {
            if (itemsControl.ItemsSource is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged -= scrollHandler; // Ensure no double subscription
                newCollection.CollectionChanged += scrollHandler;

                // Perform an initial scroll
                // --- APPLYING THE TYPE-SAFETY FIX HERE AS WELL ---
                if (itemsControl is ListBox listBox && listBox.Items.Count > 0)
                {
                    listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                }
            }
        });

        // This handler cleans up when the control is unloaded
        var unloadedHandler = new RoutedEventHandler((sender, args) =>
        {
            if (itemsControl.ItemsSource is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged -= scrollHandler;
            }
        });

        if ((bool)e.NewValue)
        {
            // Subscribe to Loaded and Unloaded events
            itemsControl.Loaded += loadedHandler;
            itemsControl.Unloaded += unloadedHandler;
        }
        else
        {
            // Unsubscribe from all events to prevent memory leaks
            itemsControl.Loaded -= loadedHandler;
            itemsControl.Unloaded -= unloadedHandler;
            if (itemsControl.ItemsSource is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= scrollHandler;
            }
        }
    }
}