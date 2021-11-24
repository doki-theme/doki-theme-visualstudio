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

    private ImageBrush? _image;

    private const string TagName = "DokiWallpaper";

    private bool _registeredListeners;

    private readonly IWpfTextView _view;

    public WallpaperAdornment(IWpfTextView view) {
      _adornmentLayer = view.GetAdornmentLayer("WallpaperAdornment");
      _adornmentLayer.RemoveAdornmentsByTag(TagName);

      _view = view;

      RefreshAdornment();

      AttemptToRegisterListeners();

      ThemeManager.Instance.DokiThemeChanged += (_, themeChangedArgs) => {
        var newDokiTheme = themeChangedArgs.Theme;
        if (newDokiTheme != null) {
          GetImageSource(newDokiTheme, newSource => {
            CreateNewImage(newSource, newDokiTheme);
            DrawWallpaper();
          });
        } else {
          RemoveWallpaperStuff();
        }
      };

      SettingsService.Instance.SettingsChanged += (_, service) => {
        if (service.DrawWallpaper) {
          DrawCurrentThemeWallpaper();
          DoStupidShit();
        } else {
          RemoveWallpaperStuff();
        }
      };

      if (!SettingsService.Instance.DrawWallpaper) return;

      DrawCurrentThemeWallpaper();
    }

    private void DrawCurrentThemeWallpaper() {
      ThemeManager.Instance.GetCurrentTheme(dokiTheme => {
        GetImageSource(dokiTheme, source => {
          CreateNewImage(source, dokiTheme);

          DrawWallpaper();

          AttemptToRegisterListeners();
        });
      });
    }

    private void RemoveWallpaperStuff() {
      RemoveWallpaper();
      AttemptToRemoveListeners();
      _image = null;
    }

    private void AttemptToRegisterListeners() {
      if (_registeredListeners) return;
      _view.LayoutChanged += OnSizeChanged;
      _view.BackgroundBrushChanged += BackgroundBrushChanged;
      _registeredListeners = true;
    }

    private void AttemptToRemoveListeners() {
      if (!_registeredListeners) return;
      _view.LayoutChanged -= OnSizeChanged;
      _view.BackgroundBrushChanged -= BackgroundBrushChanged;
      _registeredListeners = false;
    }

    private void CreateNewImage(BitmapSource source, DokiTheme dokiTheme) {
      _image = new ImageBrush(source) {
        Stretch = SettingsService.Instance.WallpaperFill,
        AlignmentX = GetAlignmentX(dokiTheme),
        AlignmentY = AlignmentY.Bottom,
        Opacity = GetOpacity(dokiTheme),
        Viewbox = new Rect(new Point(0, 0), new Size(1, 1)),
      };
    }

    private static AlignmentX GetAlignmentX(DokiTheme dokiTheme) {
      return dokiTheme.BackgroundPosition switch {
        "right" => AlignmentX.Right,
        _ => AlignmentX.Center
      };
    }

    private static double GetOpacity(DokiTheme dokiTheme) {
      var userOpacitySettings = SettingsService.Instance.WallpaperOpacity;
      var opacity = Math.Abs(userOpacitySettings + 1.0) < 0.001 ? dokiTheme.WallpaperOpacity : userOpacitySettings;
      return opacity;
    }

    private void BackgroundBrushChanged(object sender, BackgroundBrushChangedEventArgs e) {
      RefreshAdornment();
    }

    private static void GetImageSource(DokiTheme theme, Action<BitmapSource> bitmapConsumer) {
      var stickerName = theme.StickerName;
      var assetPath = $"wallpapers/{stickerName}";
      var customWallpaperImageAbsolutePath = SettingsService.Instance.CustomWallpaperImageAbsolutePath;
      if (!string.IsNullOrEmpty(customWallpaperImageAbsolutePath)) {
        ConvertToBitmap(bitmapConsumer, customWallpaperImageAbsolutePath);
      } else if (AssetManager.CanResolveSync(AssetCategory.Backgrounds, assetPath)) {
        var url = AssetManager.ResolveAssetUrl(AssetCategory.Backgrounds, assetPath) ??
                  throw new NullReferenceException("I don't have a sync wallpaper, bro.");
        ConvertToBitmap(bitmapConsumer, url);
      } else {
        Task.Run(async () => {
          var wallpaperUrl = await Task.Run(
            async () => await AssetManager.ResolveAssetUrlAsync(
              AssetCategory.Backgrounds,
              assetPath
            ));
          var wallpaperImagePath = wallpaperUrl ??
                                   throw new NullReferenceException("I don't have a async wallpaper, bro.");
          var wallpaperBitMap = ImageTools.GetBitmapSourceFromImagePath(wallpaperImagePath);
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
          bitmapConsumer(wallpaperBitMap);
        }).FileAndForget("dokiTheme/wallpaperLoad");
      }
    }

    private static void ConvertToBitmap(Action<BitmapSource> bitmapConsumer, string url) {
      var wallpaperBitMap = ImageTools.GetBitmapSourceFromImagePath(url);
      bitmapConsumer(wallpaperBitMap);
    }

    private void OnSizeChanged(object sender, EventArgs e) {
      DoStupidShit();
    }

    private void DoStupidShit() {
      var rootTextView = GetEditorView();
      if (rootTextView == null) return;

      MakeThingsAboveWallpaperTransparent();

      var prop = rootTextView.GetType().GetProperty("Background");
      var possiblyBackground = prop?.GetValue(rootTextView);

      if (!(possiblyBackground is ImageBrush background)) {
        DrawWallpaper();
      } else {
        // This is the stupidest shit, the 
        // background will artifact when the user scrolls.
        // Unless we do this everytime the layout changes,
        // the background will be big sad.
        ThreadHelper.JoinableTaskFactory.Run(async () => {
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
          ToolBox.RunSafely(() => {
            var opacity = background.Opacity;
            background.Opacity = opacity - 0.01;
            background.Opacity = opacity;
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
      if (!(property?.GetValue(dependencyObject) is Brush)) return;

      ThreadHelper.JoinableTaskFactory.Run(async () => {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ToolBox.RunSafely(() => {
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

    private void RemoveWallpaper() {
      _image = null;
      ThreadHelper.JoinableTaskFactory.Run(async () => {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var editorView = GetEditorView();
        editorView?.SetValue(Panel.BackgroundProperty, null);
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
