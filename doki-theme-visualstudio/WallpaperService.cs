using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;

namespace doki_theme_visualstudio {
  public static class WallpaperService {
    private static Window? _appMainWindow;

    public static void Init(CancellationToken cancellationToken) {
      ThreadHelper.ThrowIfNotOnUIThread();
      ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
        await ToolBox.RunSafelyAsync(async () => {
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
          _appMainWindow = Application.Current.MainWindow;
          await InstallWallpaperAsync();
        }, _ => { });
      });
    }

    private static async System.Threading.Tasks.Task InstallWallpaperAsync() {
      var applicationWindow = _appMainWindow ??
                              throw new Exception("Expected wallpaper service to be initialized!");
      var wallpaperUrl = await ThreadHelper.JoinableTaskFactory.RunAsync(
        async () => await AssetManager.ResolveAssetUrlAsync(AssetCategory.Backgrounds, "zero_two_dark.png")
      );
      var wallpaperImagePath = wallpaperUrl ??
                               throw new NullReferenceException("I don't have a wallpaper, bro.");
      var wallpaperBitMap = ViewportAdornment1.GetBitmapSourceFromImagePath(wallpaperImagePath);
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      ToolBox.RunSafely(() => {
        // get the top most parent layer to add the wallpaper too
        var appRootGrid = (Grid)applicationWindow.Template.FindName("RootGrid", applicationWindow) ??
                          throw new Exception("Expected to find root grid!");
        var wallpaperImage = new Image() {
          Source = wallpaperBitMap,
          Stretch = Stretch.Uniform,
          HorizontalAlignment = HorizontalAlignment.Right,
          VerticalAlignment = VerticalAlignment.Bottom,
          Opacity = 1.0,
        };
        Grid.SetRowSpan(wallpaperImage, 4);
        RenderOptions.SetBitmapScalingMode(wallpaperImage, BitmapScalingMode.Fant);

        appRootGrid.Children.Insert(0, wallpaperImage);

        MakeWallpaperVisible(appRootGrid);
      }, exception => { });
    }

    private static void MakeWallpaperVisible(Grid appRootGrid) {
      if (appRootGrid == null) throw new ArgumentNullException(nameof(appRootGrid));

      var dockTargets = appRootGrid.Descendants<DependencyObject>().Where(x =>
        x.GetType().FullName == "Microsoft.VisualStudio.PlatformUI.Shell.Controls.DockTarget");
      foreach (var dockTarget in dockTargets) {
        var grids = dockTarget?.Descendants<Grid>();
        if (grids == null) continue;
        foreach (var grid in grids) {
          if (grid == null) continue;
          var prop = grid.GetType().GetProperty("Background");
          var bg = prop?.GetValue(grid) as SolidColorBrush;
          if (bg == null || bg.Color.A == 0x00) continue;
          prop?.SetValue(grid, new SolidColorBrush(new Color() {
            A = 0x00,
            B = bg.Color.B,
            G = bg.Color.G,
            R = bg.Color.R
          }));
        }
      }
    }
  }
}
