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

    // The adornment that is added that contains the 
    // wallpaper.
    private readonly Canvas _editorCanvas = new Canvas() { IsHitTestVisible = false };


    private ImageBrush? _image;

    private static readonly string tagName = "DokiWallpaper";

    private readonly IWpfTextView _view;

    public WallpaperAdornment(IWpfTextView view) {
      _view = view ?? throw new ArgumentNullException(nameof(view));
      
      _adornmentLayer = view.GetAdornmentLayer("WallpaperAdornment");
      _adornmentLayer.RemoveAdornmentsByTag(tagName);
      
      GetImageSource(source => {
        _image = new ImageBrush(source) {
          Stretch = Stretch.UniformToFill,
          AlignmentX = AlignmentX.Right,
          AlignmentY = AlignmentY.Bottom,
          Opacity = 1.0,
          Viewbox = new Rect(new Point(0, 0), new Size(1,1)),
        };
      
        RefreshAdornment();
        DrawWallpaper();
        _view.LayoutChanged += OnSizeChanged;
        _view.BackgroundBrushChanged += BackgroundBrushChanged;
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
    }

    private void DrawWallpaper() {
      if (_image == null) return;
      var textView = UITools.FindChild(_editorCanvas, child => child.GetType().Name.Equals(
        "Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextView"
      ));

      textView?.SetValue(Panel.BackgroundProperty, _image);
    }

    private void RefreshAdornment() {
      _adornmentLayer.RemoveAdornmentsByTag(tagName);
      _adornmentLayer.AddAdornment(
        AdornmentPositioningBehavior.ViewportRelative,
        null,
        tagName,
        _editorCanvas,
        null
      );
    }
  }
}
