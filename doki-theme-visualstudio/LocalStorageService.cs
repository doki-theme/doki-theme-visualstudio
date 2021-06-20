using System.IO;
using Microsoft.VisualStudio.Shell;

namespace doki_theme_visualstudio {
  public class LocalStorageService {
    public static LocalStorageService Instance { get; private set; }

    public static void Init(Package package) {
      if (Instance == null) {
        Instance = new LocalStorageService(package);
      }
    }

    
    
    private readonly Package _package;

    public LocalStorageService(Package package) {
      _package = package;
    }

    public string GetAssetDirectory() {
      var userLocalDataPath = _package.UserLocalDataPath;
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
