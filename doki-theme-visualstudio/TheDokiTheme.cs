using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public static class TheDokiTheme {
    public static async Task InitializePluginAsync(Package package) {
      ThreadHelper.ThrowIfOnUIThread();

      LocalStorageService.Init(package);
      
      // depends on local storage service
      LocalAssetService.Init(package);
      
      await AssetManager.ResolveAssetUrlAsync(AssetCategory.Backgrounds, "zero_two_dark.png");
      await AssetManager.ResolveAssetUrlAsync(AssetCategory.Stickers, "/yuruCamp/rin/dark/shima_rin_dark.png");
    }
  }
}
