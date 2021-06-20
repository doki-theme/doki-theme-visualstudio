using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;

namespace doki_theme_visualstudio {

  public class Information {
    public string id {get; set;}
    public string name {get; set;}
    public string displayname {get; set;}
    public bool dark {get; set;}
    public string author {get; set;}
    public string group {get; set;}
    public string product {get; set;}
  }

  public class Sticker {
    public string name {get; set;}
    public string path {get; set;}
  }
  public class Stickers {
    public Sticker defaultSticker {get; set;}
    public Sticker? secondary {get; set;}
  }

  public class DokiTheme {
    public Information information {get; set;}
    public Dictionary<string, string> colors {get; set;}
    public Stickers stickers {get; set;}
  }
  
  
  public class ThemeManager {
    private static ThemeManager? _instance;

    private Dictionary<string, DokiTheme> _themes;

    private ThemeManager(Dictionary<string, DokiTheme> themes) {
      _themes = themes;
      
      VSColorTheme.ThemeChanged += themeArguments => {
        var defaultBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
        var defaultForeground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
        Console.WriteLine("Finna bust a nut"); 
      };
    }

    public static ThemeManager Instance =>
      _instance ?? throw new Exception("Expected local storage to be initialized!");


    public static void Init(AsyncPackage package) {
      _instance ??= new ThemeManager(ReadThemes());
    }

    private static Dictionary<string, DokiTheme> ReadThemes() {
      ThreadHelper.ThrowIfOnUIThread();
      var assembly = Assembly.GetExecutingAssembly();

      const string resourceName = "doki_theme_visualstudio.Resources.DokiThemeDefinitions.json";
      using var stream = assembly.GetManifestResourceStream(resourceName);
      using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
      using var jsonReader = new JsonTextReader(reader);
      var jsonSerializer = JsonSerializer.Create();
      var themes = jsonSerializer.Deserialize<Dictionary<string, DokiTheme>>(jsonReader);
      return themes;
    }
  }
}
