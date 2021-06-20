using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public class ThemeManager {
    public static async Task InitializePluginAsync(Package package) {
      ThreadHelper.ThrowIfOnUIThread();

      var assetDirectory = GetAssetDirectory(package);
    }

    private static string GetAssetDirectory(Package package) {
      var userLocalDataPath = package.UserLocalDataPath;
      var assetsDirectory =
        userLocalDataPath.Substring(
          0,
          userLocalDataPath.LastIndexOf(Path.DirectorySeparatorChar)
        ) + Path.DirectorySeparatorChar + "dokiThemeAssets";
      if (!Directory.Exists(assetsDirectory)) {
        Directory.CreateDirectory(assetsDirectory);
      }

      return assetsDirectory;
    }
  }
}
