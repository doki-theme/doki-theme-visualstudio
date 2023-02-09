using System;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace doki_theme_visualstudio {
  public class LocalStorageService {
    private static LocalStorageService? _instance;

    public static LocalStorageService Instance =>
      _instance ?? throw new Exception("Expected local storage to be initialized!");

    public static void Init(Package package) {
      _instance ??= new LocalStorageService(package.UserLocalDataPath);
      var assetsDirectory = _instance.GetAssetDirectory();
      if (!Directory.Exists(assetsDirectory)) {
        Directory.CreateDirectory(assetsDirectory);
      }
    }

    private readonly string _userLocalDataPath;

    private LocalStorageService(string userLocalDataPath) {
      _userLocalDataPath = userLocalDataPath;
    }

    public static void CreateDirectories(string fullAssetPath) {
      ToolBox.RunSafely(
        () => {
          Directory.CreateDirectory(fullAssetPath.Substring(0, fullAssetPath.LastIndexOf(Path.DirectorySeparatorChar)));
        }, exception => { Console.Out.WriteLine("Unable to create directories " + exception.Message); });
    }

    public string GetAssetDirectory() {
      var assetsDirectory =
        Path.Combine(_userLocalDataPath.Substring(
          0,
          _userLocalDataPath.LastIndexOf(Path.DirectorySeparatorChar)
        ), "dokiThemeAssets");
      return assetsDirectory;
    }
  }
}
