using System;
using System.IO;
using System.Net.Http;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public static class ThemeManager {
    public static async Task InitializePluginAsync(Package package) {
      ThreadHelper.ThrowIfOnUIThread();

      var assetDirectory = GetAssetDirectory(package);
      await ToolBox.RunSafelyAsync(async () => {
        using (var client = new HttpClient()) {
          var stream = await client.GetStreamAsync("https://doki.assets.unthrottled.io/backgrounds/zero_two_dark.png");
          using (var destinationFile = File.OpenWrite(
            Path.Combine(assetDirectory, "zero_two_dark.png"))) {
            await stream.CopyToAsync(destinationFile);
          }
        }
      }, _ => { });
    }

    private static string GetAssetDirectory(Package package) {
      var userLocalDataPath = package.UserLocalDataPath;
      var assetsDirectory =
        Path.Combine(userLocalDataPath.Substring(
          0,
          userLocalDataPath.LastIndexOf(Path.DirectorySeparatorChar)
        ), "dokiThemeAssets");
      
      if (!Directory.Exists(assetsDirectory)) {
        Directory.CreateDirectory(assetsDirectory);
      }

      return assetsDirectory;
    }
  }
}
