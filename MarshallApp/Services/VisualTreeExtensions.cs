using System.Windows;
using System.Windows.Media;

namespace MarshallApp.Services;

public static class VisualTreeExtensions
{
    public static T? FindVisualAncestor<T>(this DependencyObject obj) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(obj);
        while (parent != null)
        {
            if (parent is T result) return result;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}