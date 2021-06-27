using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace doki_theme_visualstudio {
  public class UITools {
    public static DependencyObject? FindParent(
      DependencyObject childDependencyObject,
      Func<DependencyObject, bool> predicate
    ) {
      var parent =
        childDependencyObject is Visual ||
        childDependencyObject is Visual3D
          ? VisualTreeHelper.GetParent(childDependencyObject)
          : LogicalTreeHelper.GetParent(childDependencyObject);
      if (parent == null) return null;

      return predicate(parent) ? parent : FindParent(parent, predicate);
    }

    public static DependencyObject? FindChild(DependencyObject applicationWindow, Func<DependencyObject, bool> func) {
      foreach (var child in applicationWindow.Descendants()) {
        if (child == null) continue;

        if (func(child)) {
          return child;
        }

        var grandChild = FindChild(child, func);
        if (grandChild != null) {
          return grandChild;
        }
      }

      return null;
    }
  }
}
