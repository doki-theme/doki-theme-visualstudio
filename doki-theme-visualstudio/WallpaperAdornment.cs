using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  internal sealed class WallpaperAdornment {
    private readonly IAdornmentLayer _adornmentLayer;

    // The adornment that is added to the editor, that allows us to get
    // the editor window to draw image on
    private readonly Canvas _editorCanvas = new Canvas { IsHitTestVisible = false };

    private ImageBrush? _image;

    private const string TagName = "DokiWallpaper";

    public WallpaperAdornment(IWpfTextView view) {
      _adornmentLayer = view.GetAdornmentLayer("WallpaperAdornment");
      _adornmentLayer.RemoveAdornmentsByTag(TagName);

      RefreshAdornment();

      GetImageSource(source => {
        _image = new ImageBrush(source) {
          Stretch = Stretch.UniformToFill,
          AlignmentX = AlignmentX.Right,
          AlignmentY = AlignmentY.Bottom,
          Opacity = 1.0,
          Viewbox = new Rect(new Point(0, 0), new Size(1, 1)),
        };

        DrawWallpaper();

        view.LayoutChanged += OnSizeChanged;
        view.BackgroundBrushChanged += BackgroundBrushChanged;
      });
    }

    private void BackgroundBrushChanged(object sender, BackgroundBrushChangedEventArgs e) {
      RefreshAdornment();
    }

    private static void GetImageSource(Action<BitmapSource> bitmapConsumer) {
      Task.Run(async () => {
        var wallpaperUrl = await Task.Run(
          async () => await AssetManager.ResolveAssetUrlAsync(AssetCategory.Backgrounds, "wallpapers/ryuko.png")
        );
        var wallpaperImagePath = wallpaperUrl ??
                                 throw new NullReferenceException("I don't have a wallpaper, bro.");
        var wallpaperBitMap = ViewportAdornment1.GetBitmapSourceFromImagePath(wallpaperImagePath);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        bitmapConsumer(wallpaperBitMap);
      }).FileAndForget("dokiTheme/wallpaperLoad");
    }

    private void OnSizeChanged(object sender, EventArgs e) {
      DoStupidShit();
    }

    private void DoStupidShit() {
      var rootTextView = GetEditorView();
      if (rootTextView == null) return;
      var possiblyBackground = rootTextView.GetValue(Panel.BackgroundProperty);

      if (!(possiblyBackground is ImageBrush)) {
        DrawWallpaper();
      }
      else {
        var background = (ImageBrush)possiblyBackground;
        ThreadHelper.JoinableTaskFactory.Run(async () => {
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
          ToolBox.RunSafely(() => {
            background.Opacity = 1 - 0.01;
            background.Opacity = 1;
          }, _ => { });
        });
      }
    }

    private DependencyObject? GetEditorView() {
      return UITools.FindParent(_editorCanvas,
        o => {
          var name = o.GetType().FullName;
          return name.Equals("Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost",
                   StringComparison.OrdinalIgnoreCase) ||
                 name.Equals("Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextView",
                   StringComparison.OrdinalIgnoreCase);
        });
    }

    private void DrawWallpaper() {
      if (_image == null) return;
      var textView = GetEditorView();

      textView?.SetValue(Panel.BackgroundProperty, _image);
    }

    private void RefreshAdornment() {
      _adornmentLayer.RemoveAdornmentsByTag(TagName);
      _adornmentLayer.AddAdornment(
        AdornmentPositioningBehavior.ViewportRelative,
        null,
        TagName,
        _editorCanvas,
        null
      );
    }
  }
}
