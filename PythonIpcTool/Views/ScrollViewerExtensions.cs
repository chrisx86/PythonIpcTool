using System.Collections.Specialized;
using System.Linq; // Required for .LastOrDefault()
using System.Windows;
using System.Windows.Controls;

namespace PythonIpcTool.Views;

public static class ScrollViewerExtensions
{
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(ScrollViewerExtensions),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // We are attaching this to an ItemsControl (like ListView)
        if (d is not ItemsControl itemsControl) return;
        if (e.NewValue is not bool autoScroll || itemsControl.ItemsSource is not INotifyCollectionChanged collection) return;

        if (autoScroll)
        {
            // Subscribe to the event when the property is set to true
            collection.CollectionChanged += OnCollectionChanged;
        }
        else
        {
            // Unsubscribe when the property is set to false
            collection.CollectionChanged -= OnCollectionChanged;
        }

        // Local function to handle the event
        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            // --- MODIFICATION START ---
            // The main fix is here. We check if the ItemsControl is a ListBox (or ListView)
            // and then safely call ScrollIntoView on the correctly typed variable.
            if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems != null)
            {
                // Performance optimization: Only scroll the last added item into view.
                // This is useful if multiple items are added in a single batch.
                var lastItem = args.NewItems[args.NewItems.Count - 1];

                if (lastItem != null)
                {
                    // Check if the control is a type that supports ScrollIntoView (like ListBox or its descendants)
                    if (itemsControl is ListBox listBox)
                    {
                        // It's safe to call ScrollIntoView now
                        listBox.ScrollIntoView(lastItem);
                    }
                    // You could add other types here if needed, e.g., 'else if (itemsControl is DataGrid dataGrid)'
                }
            }
            // --- MODIFICATION END ---
        }
    }

    private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange > 0)
        {
            (sender as ScrollViewer)?.ScrollToEnd();
        }
    }

    private static void ScrollToEnd(ScrollViewer scrollViewer) => scrollViewer.ScrollToEnd();
    public static void SetAutoScrollToEnd(UIElement element, bool value) => element.SetValue(AutoScrollToEndProperty, value);
    public static bool GetAutoScrollToEnd(UIElement element) => (bool)element.GetValue(AutoScrollToEndProperty);
}