using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public enum AssetCategory {
    Stickers,
    Backgrounds,
    Promotion,
    Misc
  }

  public static class AssetManager {
    public const string AssetSource = "https://doki.assets.unthrottled.io";
    public const string FallbackAssetSource = "https://raw.githubusercontent.com/doki-theme/doki-theme-assets/master";

    public static async Task<string?> ResolveAssetUrlAsync(AssetCategory assetCategory, string assetPath) {
      var firstAttempt = await CachedResolveAsync(assetCategory, assetPath, AssetSource);
      return firstAttempt ?? await CachedResolveAsync(assetCategory, assetPath, FallbackAssetSource);
    }

    private static async Task<string?> CachedResolveAsync(
      AssetCategory assetCategory,
      string assetPath,
      string assetSource
    ) {
      return await ResolveAssetAsync(
        assetCategory, assetPath, assetSource,
        async (localAssetPath, remoteAssetUrl) => {
          if (await LocalAssetService.Instance.HasAssetChangedAsync(localAssetPath, remoteAssetUrl)) {
            return await DownloadAndGetAssetUrlAsync(localAssetPath, remoteAssetUrl);
          }

          return File.Exists(localAssetPath) ? localAssetPath : null;
        });
    }

    private static async Task<string> DownloadAndGetAssetUrlAsync(string localAssetPath, string remoteAssetUrl) {
      return await ToolBox.RunSafelyWithResultAsync(async () => {
        LocalStorageService.CreateDirectories(localAssetPath);

        using var client = new HttpClient();
        var stream = await client.GetStreamAsync(remoteAssetUrl);
        using var destinationFile = File.OpenWrite(localAssetPath);
        await stream.CopyToAsync(destinationFile);

        return localAssetPath;
      }, exception => {
        Console.Out.WriteLine("Oh shit cannot download asset! " + exception.Message);
        return null;
      });
    }

    private static async Task<string?> ResolveAssetAsync(
      AssetCategory assetCategory,
      string assetPath,
      string assetSource,
      Func<string, string, Task<string?>> resolveAsset) {
      var localAssetPath = ConstructLocalAssetPath(assetCategory, assetPath);
      var remoteAssetPath = ConstructRemoteAssetUrl(assetCategory, assetPath, assetSource);
      return await resolveAsset(localAssetPath, remoteAssetPath);
    }

    private static string ConstructLocalAssetPath(AssetCategory assetCategory, string assetPath) {
      var localAssetPath = Path.Combine(
        LocalStorageService.Instance.GetAssetDirectory(),
        AssetCategoryName(assetCategory),
        CleanAssetPath(assetPath)
      );
      return localAssetPath;
    }

    private static string CleanAssetPath(string assetPath) {
      var cleanAssetPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
      return cleanAssetPath.StartsWith(Path.DirectorySeparatorChar.ToString())
        ? cleanAssetPath.Substring(1)
        : cleanAssetPath;
    }

    private static string AssetCategoryName(AssetCategory assetCategory) {
      return assetCategory.ToString().ToLower();
    }


    private static string ConstructRemoteAssetUrl(
      AssetCategory assetCategory,
      string assetPath,
      string assetSource
    ) {
      return assetCategory == AssetCategory.Stickers
        ? $"{assetSource}/{AssetCategoryName(assetCategory)}/jetbrains/v2{assetPath}"
        : $"{assetSource}/{AssetCategoryName(assetCategory)}/{assetPath}";
    }

    public static bool CanResolveSync(AssetCategory assetCategory, string assetPath) {
      return File.Exists(ConstructLocalAssetPath(assetCategory, assetPath));
    }

    public static string ResolveAssetUrl(AssetCategory category, string assetPath) {
      VsTaskLibraryHelper.FileAndForget(Task.Run(async () => {
        await ResolveAssetUrlAsync(category, assetPath); // makes sure asset is upto date
      }), "dokiTheme/sync/background-update");
      return ConstructLocalAssetPath(category, assetPath);
    }
  }
}
