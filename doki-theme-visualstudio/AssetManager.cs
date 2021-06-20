using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public class AssetManager {
    public static async Task ResolveAssetUrlAsync(string assetPath) {
      var assetDirectory = LocalStorageService.Instance.GetAssetDirectory();
      await ToolBox.RunSafelyWithResultAsync(async () => {
        using var client = new HttpClient();
        var stream = await client.GetStreamAsync("https://doki.assets.unthrottled.io/backgrounds/zero_two_dark.png");
        var localAssetPath = Path.Combine(assetDirectory, "zero_two_dark.png");
        using var destinationFile = File.OpenWrite(localAssetPath);
        await stream.CopyToAsync(destinationFile);
        return localAssetPath;
      }, exception => {
        ActivityLog.LogError("Oh shit cannot download asset!", exception.Message);
        return null;
      });
    }
  }
}
