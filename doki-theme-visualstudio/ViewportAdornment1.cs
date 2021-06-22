using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  /// <summary>
  ///   Adornment class that draws a square box in the top right hand corner of the viewport
  /// </summary>
  internal sealed class ViewportAdornment1 {
    private Canvas _editorCanvas = new Canvas() { IsHitTestVisible = false };

    /// <summary>
    ///   The layer for the adornment.
    /// </summary>
    private readonly IAdornmentLayer _adornmentLayer;

    /// <summary>
    ///   Adornment image
    /// </summary>
    private Image? _image;

    /// <summary>
    ///   Text view to add the adornment on.
    /// </summary>
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
      _adornmentLayer.AddAdornment(
        AdornmentPositioningBehavior.ViewportRelative,
        null,
        "DokiTheme",
        _editorCanvas,
        null
      );

      GetImageSource(source => {
        _image = new Image {
          Source = source,
          Stretch = Stretch.Uniform,
          HorizontalAlignment = HorizontalAlignment.Right,
          VerticalAlignment = VerticalAlignment.Bottom,
          Opacity = 1.0,
          IsHitTestVisible = false
        };
        _view.LayoutChanged += OnSizeChanged;
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

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.None;
        bitmap.UriSource = new Uri(imagePath!, UriKind.RelativeOrAbsolute);
        bitmap.EndInit();
        bitmap.Freeze();
        var finalBitmap = ConvertToDpi96(bitmap);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        bitmapConsumer(finalBitmap);
      });
    }
    
    private static BitmapSource ConvertToDpi96(BitmapSource source) {
      const int dpi = 96;
      var width = source.PixelWidth;
      var height = source.PixelHeight;

      var stride = width * 4;
      var pixelData = new byte[stride * height];
      source.CopyPixels(pixelData, stride, 0);

      return BitmapSource.Create(
        width, height, 
        dpi, dpi, 
        PixelFormats.Bgra32, null, 
        pixelData, stride
        );
    }



    private void OnSizeChanged(object sender, EventArgs e) {
      if (_image == null) return;
      
      Grid.SetRowSpan(_image, 4);
      RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.Fant);

    }
  }
}
