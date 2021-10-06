using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace doki_theme_visualstudio {
  public static class TheDokiTheme {
    public static async Task InitializePluginAsync(AsyncPackage package, CancellationToken cancellationToken) {
      ThreadHelper.ThrowIfOnUIThread();
      
      SettingsService.Init(package);
      ThemeManager.Init(package);
      LocalStorageService.Init(package);
      
      // depends on local storage service
      LocalAssetService.Init(package);
    }
  }
}
