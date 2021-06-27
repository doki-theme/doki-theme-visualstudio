using System;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace doki_theme_visualstudio {
  public static class WallpaperService {

    public static async System.Threading.Tasks.Task InstallEditorWallpaperAsync(IWpfTextView textView) {
      var wallpaperUrl = await System.Threading.Tasks.Task.Run(
        async () => await AssetManager.ResolveAssetUrlAsync(AssetCategory.Backgrounds, "wallpapers/ryuko.png")
      );
      var wallpaperImagePath = wallpaperUrl ??
                               throw new NullReferenceException("I don't have a wallpaper, bro.");
      var wallpaperBitMap = ViewportAdornment1.GetBitmapSourceFromImagePath(wallpaperImagePath);
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      ToolBox.RunSafely(() => {
        var wallpaperImage = new ImageBrush(wallpaperBitMap) {
          Stretch = Stretch.UniformToFill,
          AlignmentX = AlignmentX.Right,
          AlignmentY = AlignmentY.Bottom,
          Opacity = 1.0,
        };
        textView.Background = wallpaperImage;
      }, exception => { });
    }
  }
}
