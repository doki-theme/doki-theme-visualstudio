using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace doki_theme_visualstudio {
  internal static class ImageTools {
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
  }
}
