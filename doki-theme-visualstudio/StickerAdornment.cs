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

    public StickerAdornment(IWpfTextView view) {
      _view = view ?? throw new ArgumentNullException(nameof(view));

      _adornmentLayer = view.GetAdornmentLayer("StickerAdorment");
      _adornmentLayer.RemoveAdornmentsByTag(TagName);

      GetImageSource(source => {
        _image = new Image {
          Source = source,
          Opacity = 1.0
        };
        
        DrawImage();
        // todo: fancy animation
        // var fadeInAnimation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(500));
        // _image.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        // fadeInAnimation.Completed += (_, __) => {
          // _image.Opacity = 1.0;
          _view.LayoutChanged += OnSizeChanged;
        // };
      });
    }

    private static void GetImageSource(Action<BitmapSource> bitmapConsumer) {
      Task.Run(async () => {
        var imagePath = await AssetManager.ResolveAssetUrlAsync(
          AssetCategory.Stickers,
          ThemeManager.Instance.ThemeById("5fb9c0a4-e613-457c-97a5-6204f9076cef")!.StickerPath
        );
        if (string.IsNullOrEmpty(imagePath)) return;

        var finalBitmap = ImageTools.GetBitmapSourceFromImagePath(imagePath!);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        bitmapConsumer(finalBitmap);
      }).FileAndForget("dokiTheme/StickerLoad");
    }

    private void OnSizeChanged(object sender, EventArgs e) {
      DrawImage();
    }

    private void DrawImage() {
      if (_image == null) return;

      _adornmentLayer.RemoveAdornmentsByTag(TagName);

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
