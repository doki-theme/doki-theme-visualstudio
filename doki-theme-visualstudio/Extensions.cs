using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace doki_theme_visualstudio {
  public static class Extensions {
    public static IEnumerable<DependencyObject> Children(this DependencyObject obj) {
      if (obj == null)
        throw new ArgumentNullException("obj");

      var count = VisualTreeHelper.GetChildrenCount(obj);
      if (count == 0)
        yield break;

      for (var i = 0; i < count; i++) {
        var child = VisualTreeHelper.GetChild(obj, i);
        yield return child;
      }
    }

    public static IEnumerable<DependencyObject> Descendants(this DependencyObject obj) {
      if (obj == null)
        throw new ArgumentNullException("obj");

      foreach (var child in obj.Children()) {
        yield return child;
        foreach (var grandChild in child.Descendants())
          yield return grandChild;
      }
    }

    public static IEnumerable<T> Descendants<T>(this DependencyObject obj)
      where T : DependencyObject {
      return obj.Descendants().OfType<T>();
    }
  }
}
