using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  internal sealed class ViewportAdornment1 {
    private readonly IAdornmentLayer _adornmentLayer;

    private Image? _image;

    private readonly IWpfTextView _view;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ViewportAdornment1" /> class.
    ///   Creates a square image and attaches an event handler to the layout changed event that
    ///   adds the the square in the upper right-hand corner of the TextView via the adornment layer
    /// </summary>
    /// <param name="view">The <see cref="IWpfTextView" /> upon which the adornment will be drawn</param>
    public ViewportAdornment1(IWpfTextView view) {
      _view = view ?? throw new ArgumentNullException(nameof(view));

      _adornmentLayer = view.GetAdornmentLayer("ViewportAdornment1");
      _adornmentLayer.RemoveAdornmentsByTag("DokiTheme");

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
          // ThemeManager.Instance.ThemeById("8c99ec4b-fda0-4ab7-95ad-a6bf80c3924b")!.StickerName
        );
        if (string.IsNullOrEmpty(imagePath)) return;

        var finalBitmap = GetBitmapSourceFromImagePath(imagePath!);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        bitmapConsumer(finalBitmap);
      });
    }

    public static BitmapSource GetBitmapSourceFromImagePath(string imagePath) {
      var bitmap = new BitmapImage();
      bitmap.BeginInit();
      bitmap.CacheOption = BitmapCacheOption.OnLoad;
      bitmap.CreateOptions = BitmapCreateOptions.None;
      bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
      bitmap.EndInit();
      bitmap.Freeze();
      var finalBitmap = ConvertToDpi96(bitmap);
      return finalBitmap;
    }

    private static BitmapSource ConvertToDpi96(BitmapSource source) {
      const int dpi = 96;
      var width = source.PixelWidth;
      var height = source.PixelHeight;

      var stride = width * 4;
      var pixelData = new byte[stride * height];
      source.CopyPixels(pixelData, stride, 0);
      var convertedBitmap = BitmapSource.Create(
        width, height,
        dpi, dpi,
        PixelFormats.Bgra32, null,
        pixelData, stride
      );
      convertedBitmap.Freeze();
      return convertedBitmap;
    }


    private void OnSizeChanged(object sender, EventArgs e) {
      DrawImage();
    }

    private void DrawImage() {
      if (_image == null) return;

      _adornmentLayer.RemoveAdornmentsByTag("DokiTheme");

      // place in lower right hand corner
      Canvas.SetLeft(_image, _view.ViewportRight - _image.ActualWidth);
      Canvas.SetTop(_image, _view.ViewportBottom - _image.ActualHeight);

      // add image to editor window
      _adornmentLayer.AddAdornment(
        AdornmentPositioningBehavior.ViewportRelative,
        null,
        "DokiTheme",
        _image,
        null
      );
    }
  }
}
