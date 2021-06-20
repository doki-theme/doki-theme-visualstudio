using System;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public class ThemeManager {

    public static async Task InitializePluginAsync(Package package) {
      ThreadHelper.ThrowIfOnUIThread();

      var assetDirectory = GetAssetDirectory(package);
      // check if exists
      await Console.Out.WriteLineAsync("Ayy lmao: " + assetDirectory);
    }

    private static string GetAssetDirectory(Package package) {
      var userLocalDataPath = package.UserLocalDataPath;
      return userLocalDataPath.Substring(0, userLocalDataPath.LastIndexOf(Path.DirectorySeparatorChar)) +
             Path.DirectorySeparatorChar + "dokiThemeAssets";
    }
  }
}
