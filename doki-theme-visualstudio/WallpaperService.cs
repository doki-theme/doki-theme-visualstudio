using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace doki_theme_visualstudio {
  public static class WallpaperService {
    private static Window? _appMainWindow;

    public static void Init(CancellationToken cancellationToken) {
      ThreadHelper.ThrowIfNotOnUIThread();
      Application.Current.MainWindow.Loaded += (mainWindow, _) => {
        _appMainWindow = (Window)mainWindow;
        InstallWallpaperAsync().FileAndForget("dokiTheme/installWallpaper/loaded");
      };
      ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
        await ToolBox.RunSafelyAsync(async () => {
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
          _appMainWindow = Application.Current.MainWindow;
          InstallWallpaperAsync().FileAndForget("dokiTheme/installWallpaper/init");
        }, _ => { });
      });
    }

    private static async System.Threading.Tasks.Task InstallWallpaperAsync() {
      var applicationWindow = _appMainWindow ??
                              throw new Exception("Expected wallpaper service to be initialized!");
      var wallpaperUrl = await System.Threading.Tasks.Task.Run(
        async () => await AssetManager.ResolveAssetUrlAsync(AssetCategory.Backgrounds, "wallpapers/ryuko.png")
      );
      var wallpaperImagePath = wallpaperUrl ??
                               throw new NullReferenceException("I don't have a wallpaper, bro.");
      var wallpaperBitMap = ViewportAdornment1.GetBitmapSourceFromImagePath(wallpaperImagePath);
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      ToolBox.RunSafely(() => {
        // get the top most parent layer to add the wallpaper too
        var appRootGrid = (Grid)applicationWindow.Template.FindName("RootGrid", applicationWindow) ??
                          throw new Exception("Expected to find root grid!");
        var wallpaperImage = new Image {
          Source = wallpaperBitMap,
          Stretch = Stretch.UniformToFill,
          HorizontalAlignment = HorizontalAlignment.Right,
          VerticalAlignment = VerticalAlignment.Bottom,
          Opacity = 1.0,
        };
        Grid.SetRowSpan(wallpaperImage, 4);
        RenderOptions.SetBitmapScalingMode(wallpaperImage, BitmapScalingMode.Fant);

        appRootGrid.Children.Insert(0, wallpaperImage);
      }, exception => { });
    }

    public static async System.Threading.Tasks.Task InstallEditorWallpaperAsync(IWpfTextView textView) {
      var applicationWindow = FindParent(
        textView as DependencyObject, 
        parent => parent.GetType().Name.Equals("Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextViewHost", StringComparison.OrdinalIgnoreCase)
      ) ?? throw new NullReferenceException("Couldn't find parent bro");
      var wallpaperUrl = await System.Threading.Tasks.Task.Run(
        async () => await AssetManager.ResolveAssetUrlAsync(AssetCategory.Backgrounds, "wallpapers/ryuko.png")
      );
      var wallpaperImagePath = wallpaperUrl ??
                               throw new NullReferenceException("I don't have a wallpaper, bro.");
      var wallpaperBitMap = ViewportAdornment1.GetBitmapSourceFromImagePath(wallpaperImagePath);
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      ToolBox.RunSafely(() => {
        // get the top most parent layer to add the wallpaper too
        var appRootGrid = (Grid?)FindChild(applicationWindow, depObject => false) ??
                          throw new Exception("Expected to find editor grid!");
        var wallpaperImage = new Image {
          Source = wallpaperBitMap,
          Stretch = Stretch.UniformToFill,
          HorizontalAlignment = HorizontalAlignment.Right,
          VerticalAlignment = VerticalAlignment.Bottom,
          Opacity = 1.0,
        };
        Grid.SetRowSpan(wallpaperImage, 4);
        RenderOptions.SetBitmapScalingMode(wallpaperImage, BitmapScalingMode.Fant);

        appRootGrid.Children.Insert(0, wallpaperImage);
      }, exception => { });
    }

    private static DependencyObject? FindChild(DependencyObject applicationWindow, Func<DependencyObject, bool> func) {
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

    private static DependencyObject? FindParent(
      DependencyObject textView,
      Func<DependencyObject, bool> predicate
      ) {
      var parent = LogicalTreeHelper.GetParent(textView);
      if (parent == null) return null;

      return predicate(parent) ? parent : FindParent(parent, predicate);
    }
  }
}
