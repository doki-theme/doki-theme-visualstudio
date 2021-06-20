using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public static class TheDokiTheme {
    public static async Task InitializePluginAsync(Package package) {
      ThreadHelper.ThrowIfOnUIThread();

      LocalStorageService.Init(package);
      await AssetManager.ResolveAssetUrlAsync("zeroTwo");

    }
  }
}
