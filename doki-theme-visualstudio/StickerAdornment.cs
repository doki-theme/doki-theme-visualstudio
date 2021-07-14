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

    private readonly IWpfTextView _view;

    private bool _registeredLayoutListener;

    public StickerAdornment(IWpfTextView view) {
      _view = view ?? throw new ArgumentNullException(nameof(view));

      _adornmentLayer = view.GetAdornmentLayer("StickerAdornment");
      RemoveAdornment();

      ThemeManager.Instance.DokiThemeChanged += (_, themeChangedArgs) => {
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

      SettingsService.Instance.SettingsChanged += (_, service) => {
        if (service.DrawSticker) {
          if (_image != null) return;
          DrawCurrentSticker();
        } else {
          RemoveStickerStuff();
        }
      };

      if (!SettingsService.Instance.DrawSticker) return;

      DrawCurrentSticker();
    }

    private void RemoveStickerStuff() {
      RemoveAdornment();
      AttemptToRemoveLayoutListener();
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
      if (AssetManager.CanResolveSync(AssetCategory.Stickers, themeStickerPath)) {
        var stickerImagePath = AssetManager.ResolveAssetUrl(AssetCategory.Stickers, themeStickerPath);
        var stickerBitMap = ImageTools.GetBitmapSourceFromImagePath(stickerImagePath);
        bitmapConsumer(stickerBitMap);
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

    private void OnSizeChanged(object sender, EventArgs e) {
      DrawImage();
    }

    private void DrawImage() {
      if (_image == null) return;

      RemoveAdornment();

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
