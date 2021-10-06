using System;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace doki_theme_visualstudio {
  public class LocalStorageService {
    private static LocalStorageService? _instance;

    public static LocalStorageService Instance =>
      _instance ?? throw new Exception("Expected local storage to be initialized!");

    public static void Init(Package package) {
      _instance ??= new LocalStorageService(package);
      var assetsDirectory = _instance.GetAssetDirectory();
      if (!Directory.Exists(assetsDirectory)) {
        Directory.CreateDirectory(assetsDirectory);
      }
    }

    private readonly Package _package;

    private LocalStorageService(Package package) {
      _package = package;
    }

    public static void CreateDirectories(string fullAssetPath) {
      ToolBox.RunSafely(
        () => {
          Directory.CreateDirectory(fullAssetPath.Substring(0, fullAssetPath.LastIndexOf(Path.DirectorySeparatorChar)));
        }, exception => { Console.Out.WriteLine("Unable to create directories " + exception.Message); });
    }

    public string GetAssetDirectory() {
      var userLocalDataPath = _package.UserLocalDataPath;
      var assetsDirectory =
        Path.Combine(userLocalDataPath.Substring(
          0,
          userLocalDataPath.LastIndexOf(Path.DirectorySeparatorChar)
        ), "dokiThemeAssets");
      return assetsDirectory;
    }
  }
}
