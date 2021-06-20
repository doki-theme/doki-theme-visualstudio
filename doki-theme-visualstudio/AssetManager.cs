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

  public class AssetManager {
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
        async (localAssetPath, remoteAssetUrl) =>
          await ToolBox.RunSafelyWithResultAsync(async () => {
            LocalStorageService.CreateDirectories(localAssetPath);
            
            using var client = new HttpClient();
            var stream = await client.GetStreamAsync(remoteAssetUrl);
            using var destinationFile = File.OpenWrite(localAssetPath);
            await stream.CopyToAsync(destinationFile);
            
            return localAssetPath;
          }, exception => {
            ActivityLog.LogError("Oh shit cannot download asset!", exception.Message);
            return null;
          }));
    }

    private static async Task<string?> ResolveAssetAsync(
      AssetCategory assetCategory,
      string assetPath,
      string assetSource,
      Func<string, string, Task<string?>> resolveAsset) {
      var localAssetPath = Path.Combine(
        LocalStorageService.Instance.GetAssetDirectory(),
        AssetCategoryName(assetCategory),
        assetPath
      );
      var remoteAssetPath = ConstructRemoteAssetUrl(assetCategory, assetPath, assetSource);
      return await resolveAsset(localAssetPath, remoteAssetPath);
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
        ? $"{assetSource}/${AssetCategoryName(assetCategory)}/jetbrains/v2${assetPath}"
        : $"{assetSource}/${AssetCategoryName(assetCategory)}/${assetPath}";
    }
  }
}
