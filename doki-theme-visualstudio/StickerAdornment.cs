using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  internal sealed class StickerAdornment {
    private readonly IAdornmentLayer _adornmentLayer;

    private Image? _image;

    private const string TagName = "DokiSticker";

    private IWpfTextView _view;

    private bool _registeredLayoutListener;

    private double _stickerSize;

    public StickerAdornment(IWpfTextView view) {
      _view = view ?? throw new ArgumentNullException(nameof(view));

      _adornmentLayer = view.GetAdornmentLayer("StickerAdornment");
      RemoveAdornment();

      EventHandler<ThemeChangedArgs> themeChangedCallback = (_, themeChangedArgs) => {
        var newDokiTheme = themeChangedArgs.Theme;
        if (newDokiTheme != null && SettingsService.Instance.DrawSticker) {
          GetImageSource(newDokiTheme, newSource => {
            CreateNewImage(newSource);
            DrawImage();
            AttemptToRegisterLayoutListener();
          });
        } else {
          RemoveStickerStuff();
        }
      };

      ThemeManager.Instance.DokiThemeChanged += themeChangedCallback;

      EventHandler<SettingsService> settingsChangedCallback = (_, service) => {
        if (service.DrawSticker) {
          DrawCurrentSticker();
          DoStupidShit();
        } else {
          RemoveStickerStuff();
        }

        _stickerSize = service.StickerRelativeSize;
      };

      SettingsService.Instance.SettingsChanged += settingsChangedCallback;

      EventHandler textviewClosed = null;
      textviewClosed = (_, args) => {
          // Clean up all our references as the textview has closed
          ThemeManager.Instance.DokiThemeChanged -= themeChangedCallback;
          SettingsService.Instance.SettingsChanged -= settingsChangedCallback;

          _view.Closed -= textviewClosed;
          _view = null;
      };

      _view.Closed += textviewClosed;

      _stickerSize = SettingsService.Instance.StickerRelativeSize;

      if (!SettingsService.Instance.DrawSticker) return;

      DrawCurrentSticker();
    }

    // Causes the sticker to re-render
    // which will make it show up. If
    // we don't do this the sticker looks like
    // it disappears :(
    private void DoStupidShit() {
      var drawnImage = _image;
      if (drawnImage == null) return;
      ThreadHelper.JoinableTaskFactory.Run(async () => {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ToolBox.RunSafely(() => {
          drawnImage.Opacity = 0.99;
          drawnImage.Opacity = 1.0;
        }, _ => { });
      });
    }

    private void RemoveStickerStuff() {
      RemoveAdornment();
      AttemptToRemoveLayoutListener();
      _image = null;
    }

    private void DrawCurrentSticker() {
      ThemeManager.Instance.GetCurrentTheme(dokiTheme => {
        GetImageSource(dokiTheme, source => {
          CreateNewImage(source);
          DrawImage();
          AttemptToRegisterLayoutListener();
        });
      });
    }

    private void RemoveAdornment() {
      _adornmentLayer.RemoveAdornmentsByTag(TagName);
    }

    private void AttemptToRegisterLayoutListener() {
      if (_registeredLayoutListener) return;
      _view.LayoutChanged += OnSizeChanged;
      _registeredLayoutListener = true;
    }

    private void AttemptToRemoveLayoutListener() {
      if (!_registeredLayoutListener) return;
      _view.LayoutChanged -= OnSizeChanged;
      _registeredLayoutListener = false;
    }

    private void CreateNewImage(BitmapSource source) {
      _image = new Image {
        Source = source,
        Opacity = 1.0
      };
    }

    private static void GetImageSource(DokiTheme theme, Action<BitmapSource> bitmapConsumer) {
      var themeStickerPath = theme.StickerPath;
      var customStickerImageAbsolutePath = SettingsService.Instance.CustomStickerImageAbsolutePath;
      if (!string.IsNullOrEmpty(customStickerImageAbsolutePath)) {
        ConvertToBitMap(bitmapConsumer, customStickerImageAbsolutePath);
      } else if (AssetManager.CanResolveSync(AssetCategory.Stickers, themeStickerPath)) {
        var stickerImagePath = AssetManager.ResolveAssetUrl(AssetCategory.Stickers, themeStickerPath);
        ConvertToBitMap(bitmapConsumer, stickerImagePath);
      } else {
        Task.Run(async () => {
          var stickerImagePath = await AssetManager.ResolveAssetUrlAsync(
            AssetCategory.Stickers,
            themeStickerPath
          );
          if (string.IsNullOrEmpty(stickerImagePath)) return;

          var stickerBitMap = ImageTools.GetBitmapSourceFromImagePath(stickerImagePath!);
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
          bitmapConsumer(stickerBitMap);
        }).FileAndForget("dokiTheme/StickerLoad");
      }
    }

    private static void ConvertToBitMap(Action<BitmapSource> bitmapConsumer, string? stickerImagePath) {
      var stickerBitMap = ImageTools.GetBitmapSourceFromImagePath(stickerImagePath);
      bitmapConsumer(stickerBitMap);
    }

    private void OnSizeChanged(object sender, EventArgs e) {
      DrawImage();
    }

    private void DrawImage() {
      if (_image == null) return;

      RemoveAdornment();

      if (_stickerSize >= 0) {
        var aspectRatio = _image.Width / _image.Height;
        var usableWidth = _view.ViewportWidth * _stickerSize;
        var usableHeight = usableWidth * aspectRatio;
        _image.Width = usableWidth;
        _image.Height = usableHeight;
      }

      // place in lower right hand corner
      Canvas.SetLeft(_image, _view.ViewportRight - _image.ActualWidth);
      Canvas.SetTop(_image, _view.ViewportBottom - _image.ActualHeight);

      // add image to editor window
      _adornmentLayer.AddAdornment(
        AdornmentPositioningBehavior.ViewportRelative,
        null,
        TagName,
        _image,
        null
      );
    }
  }
}
