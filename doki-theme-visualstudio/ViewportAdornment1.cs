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
    /// <summary>
    ///   The width of the square box.
    /// </summary>
    private const double AdornmentWidth = 30;

    /// <summary>
    ///   The height of the square box.
    /// </summary>
    private const double AdornmentHeight = 30;

    /// <summary>
    ///   Distance from the viewport top to the top of the square box.
    /// </summary>
    private const double TopMargin = 30;

    /// <summary>
    ///   Distance from the viewport right to the right end of the square box.
    /// </summary>
    private const double RightMargin = 30;

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
      if (view == null) throw new ArgumentNullException("view");

      this._view = view;

      _adornmentLayer = view.GetAdornmentLayer("ViewportAdornment1");

      GetImageSource(source => {
        _image = new Image {
          Source = source,
          Stretch = Stretch.None,
          
        };
        this._view.LayoutChanged += OnSizeChanged;
      });
    }

    private static void GetImageSource(Action<BitmapSource> bitmapConsumer) {
      Task.Run(async () => {
        var imagePath = await AssetManager.ResolveAssetUrlAsync(
          AssetCategory.Stickers,
          ThemeManager.Instance.ThemeById("5fb9c0a4-e613-457c-97a5-6204f9076cef")!.StickerPath
        );
        if (string.IsNullOrEmpty(imagePath)) return;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.None;
        bitmap.UriSource = new Uri(imagePath!, UriKind.RelativeOrAbsolute);
        bitmap.EndInit();
        bitmap.Freeze();
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        bitmapConsumer(bitmap);
      });
    }


    /// <summary>
    ///   Event handler for viewport layout changed event. Adds adornment at the top right corner of the viewport.
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void OnSizeChanged(object sender, EventArgs e) {
      if(_image == null) return;

      // Clear the adornment layer of previous adornments
      _adornmentLayer.RemoveAllAdornments();

      // Place the image in the top right hand corner of the Viewport
      Canvas.SetLeft(_image, _view.ViewportRight - RightMargin - AdornmentWidth);
      Canvas.SetTop(_image, _view.ViewportTop + TopMargin);

      // Add the image to the adornment layer and make it relative to the viewport
      _adornmentLayer.AddAdornment(
        AdornmentPositioningBehavior.ViewportRelative,
        null,
        null,
        _image,
        null
      );
    }
  }
}
