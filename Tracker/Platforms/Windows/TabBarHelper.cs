using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tracker.Platforms.Windows;

public static class TabBarHelper
{
    public static void CustomizeTabBar(NavigationView navigationView)
    {
        if (navigationView == null)
            return;

        navigationView.Loaded += (s, e) =>
        {
            // Configure NavigationView for top mode (will be repositioned by BottomTabBarHandler)
            navigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;

            // Get menu items
            var footerItems = navigationView.FooterMenuItems;
            var menuItems = footerItems.Count > 0 ? footerItems : navigationView.MenuItems;

            if (menuItems.Count > 0)
            {
                // Initial setup and on size changed
                void UpdateTabWidths()
                {
                    var totalWidth = navigationView.ActualWidth;
                    if (totalWidth > 0 && menuItems.Count > 0)
                    {
                        var tabWidth = totalWidth / menuItems.Count;

                        foreach (var item in menuItems)
                        {
                            if (item is NavigationViewItem navItem)
                            {
                                navItem.Width = tabWidth;
                                navItem.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
                                navItem.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;

                                // Find the TextBlock and make it bold and larger
                                var textBlock = FindVisualChild<TextBlock>(navItem);
                                if (textBlock != null)
                                {
                                    textBlock.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                                    textBlock.FontSize = 15.45; // 15 * 1.03 = 15.45
                                    textBlock.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                                }
                            }
                        }
                    }
                }

                // Update immediately
                UpdateTabWidths();

                // Update on window resize
                navigationView.SizeChanged += (sender, args) => UpdateTabWidths();
            }
        };
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            return null;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T typedChild)
                return typedChild;

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        
        return null;
    }
}
