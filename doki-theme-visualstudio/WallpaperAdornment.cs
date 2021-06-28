using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  
  // Wallpaper works by attaching a background image to the 
  // WpfMultiViewHost which is the parent of the text editor view.
  // This is important, because this allows the background image to be anchored
  // Appropriately, it is also more gooder for when the user scrolls.
  // Which is pain in the ass, which require me to do stupid shit,
  // See <code>DoStupidShit</code> for more details.
  internal sealed class WallpaperAdornment {
    private readonly IAdornmentLayer _adornmentLayer;

    // The adornment that is added to the editor as a leaf, that allows us to get
    // traverse up the tree and make things transparent so that
    // we can show the background image
    private readonly Canvas _editorCanvas = new Canvas { IsHitTestVisible = false };
    private const string EditorViewClassName = "Microsoft.VisualStudio.Editor.Implementation.WpfMultiViewHost";

    private Dictionary<int, DependencyObject> _defaultThemeColor = new Dictionary<int, DependencyObject>();

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
        var stickerName = ThemeManager.Instance.ThemeById("5fb9c0a4-e613-457c-97a5-6204f9076cef")!.StickerName;
        var wallpaperUrl = await Task.Run(
          async () => await AssetManager.ResolveAssetUrlAsync(
            AssetCategory.Backgrounds, 
            $"wallpapers/{stickerName}"
            )
        );
        var wallpaperImagePath = wallpaperUrl ??
                                 throw new NullReferenceException("I don't have a wallpaper, bro.");
        var wallpaperBitMap = ImageTools.GetBitmapSourceFromImagePath(wallpaperImagePath);
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

      MakeThingsAboveWallpaperTransparent();

      var prop = rootTextView.GetType().GetProperty("Background");
      var possiblyBackground = prop.GetValue(rootTextView);

      if (!(possiblyBackground is ImageBrush)) {
        DrawWallpaper();
      }
      else {
        var background = (ImageBrush)possiblyBackground;
        
        // This is the stupidest shit, the 
        // background will artifact when the user scrolls.
        // Unless we do this everytime the layout changes,
        // The background will be big sad.
        ThreadHelper.JoinableTaskFactory.Run(async () => {
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
          ToolBox.RunSafely(() => {
            background.Opacity = 1 - 0.01;
            background.Opacity = 1;
          }, _ => { });
        });
      }
    }

    private void MakeThingsAboveWallpaperTransparent() {
      UITools.FindParent(_editorCanvas, parent => {
        if (parent.GetType().FullName
          .Equals(EditorViewClassName)) return true;

        SetBackgroundToTransparent(parent);

        return false;
      });
    }

    private void SetBackgroundToTransparent(DependencyObject dependencyObject) {
      var property = dependencyObject.GetType().GetProperty("Background");
      if (!(property?.GetValue(dependencyObject) is Brush current)) return;

      ThreadHelper.JoinableTaskFactory.Run(async () => {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ToolBox.RunSafely(() => {
          if (_defaultThemeColor.All(x => x.Key != dependencyObject.GetHashCode())) {
            _defaultThemeColor[dependencyObject.GetHashCode()] = current;
          }

          property.SetValue(dependencyObject, Brushes.Transparent);
        }, _ => { });
      });
    }

    private DependencyObject? GetEditorView() {
      return UITools.FindParent(_editorCanvas,
        o => o.GetType().FullName.Equals(EditorViewClassName,
          StringComparison.OrdinalIgnoreCase));
    }

    private void DrawWallpaper() {
      if (_image == null) return;
      ThreadHelper.JoinableTaskFactory.Run(async () => {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var editorView = GetEditorView();
        editorView?.SetValue(Panel.BackgroundProperty, _image);
      });
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
