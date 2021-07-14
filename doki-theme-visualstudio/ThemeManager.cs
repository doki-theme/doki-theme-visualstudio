using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;

namespace doki_theme_visualstudio {
  public class Information {
    public string id { get; set; }
    public string name { get; set; }
    public string displayname { get; set; }
    public bool dark { get; set; }
    public string author { get; set; }
    public string group { get; set; }
    public string product { get; set; }
  }

  public class Sticker {
    public string name { get; set; }
    public string path { get; set; }
  }

  public class Stickers {
    public Sticker defaultSticker { get; set; }
    public Sticker? secondary { get; set; }
  }

  public class DokiThemeDefinition {
    public Information information { get; set; }
    public Dictionary<string, string> colors { get; set; }
    public Stickers stickers { get; set; }
  }

  public class DokiTheme {
    private readonly DokiThemeDefinition _definition;

    public DokiTheme(DokiThemeDefinition definition) {
      _definition = definition;
    }

    public string StickerPath => _definition.stickers.defaultSticker.path;
    public string StickerName => _definition.stickers.defaultSticker.name;
  }

  public class ThemeChangedArgs {
    public ThemeChangedArgs(DokiTheme? theme) {
      Theme = theme;
    }

    public DokiTheme? Theme { get; }
  }

  public class ThemeManager {
    private static ThemeManager? _instance;

    private Dictionary<string, DokiThemeDefinition> _themes;
    private Dictionary<string, DokiTheme> _themesByColors;

    private DokiTheme? _currentTheme;

    private ThemeManager(Dictionary<string, DokiThemeDefinition> themes) {
      _themes = themes;

      _themesByColors = themes.ToDictionary(pair => {
        var colors = pair.Value.colors;
        return $"{colors["accentColor"].ToLower()}{colors["textEditorBackground"].ToLower()}";
      }, pair => new DokiTheme(pair.Value));
      VSColorTheme.ThemeChanged += themeArguments => {
        var currentDokiTheme = GetCurrentDokiTheme();
        if (currentDokiTheme == _currentTheme) return;
        _currentTheme = currentDokiTheme;
        DokiThemeChanged?.Invoke(this, new ThemeChangedArgs(currentDokiTheme));
      };

      _currentTheme = GetCurrentDokiTheme();
    }

    public void GetCurrentTheme(Action<DokiTheme> themeConsumer) {
      var currentTheme = _currentTheme;
      if (currentTheme != null) {
        themeConsumer(currentTheme);
      }
    }

    private DokiTheme? GetCurrentDokiTheme() {
      var accentColor = VSColorTheme.GetThemedColor(EnvironmentColors.PanelHyperlinkColorKey);
      var textEditorBackground = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
      var colorKey = accentColor.ToHexString() +
                     textEditorBackground.ToHexString();

      var key = colorKey.ToLower();
      return _themesByColors.ContainsKey(key) ? _themesByColors[key] : null;
    }

    public static ThemeManager Instance =>
      _instance ?? throw new Exception("Expected local storage to be initialized!");

    public static void Init(AsyncPackage package) {
      _instance ??= new ThemeManager(ReadThemes());
    }

    public event EventHandler<ThemeChangedArgs>? DokiThemeChanged;

    public DokiTheme? ThemeById(string themeId) {
      var dokiThemeDefinition = _themes[themeId];
      return dokiThemeDefinition == null ? null : new DokiTheme(dokiThemeDefinition);
    }

    private static Dictionary<string, DokiThemeDefinition> ReadThemes() {
      ThreadHelper.ThrowIfOnUIThread();
      var assembly = Assembly.GetExecutingAssembly();

      const string resourceName = "doki_theme_visualstudio.Resources.DokiThemeDefinitions.json";
      using var stream = assembly.GetManifestResourceStream(resourceName);
      using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
      using var jsonReader = new JsonTextReader(reader);
      var jsonSerializer = JsonSerializer.Create();
      var themes = jsonSerializer.Deserialize<Dictionary<string, DokiThemeDefinition>>(jsonReader);
      return themes;
    }
  }
}
