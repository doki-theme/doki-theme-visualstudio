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
      while (true) {
        var parent = childDependencyObject is Visual || childDependencyObject is Visual3D
          ? VisualTreeHelper.GetParent(childDependencyObject)
          : LogicalTreeHelper.GetParent(childDependencyObject);
        if (parent == null) return null;

        if (predicate(parent)) return parent;
        childDependencyObject = parent;
      }
    }

  }
}
