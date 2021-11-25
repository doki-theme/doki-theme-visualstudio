using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Media;

namespace doki_theme_visualstudio {
  public class SettingsService {
    private static SettingsService? _instance;

    public static SettingsService Instance =>
      _instance ?? throw new Exception("Expected settings to be initialized!");

    public static void Init(Package package) {
      _instance ??= new SettingsService(package);
    }

    public static bool IsInitialized() => _instance != null;

    private readonly Package _package;

    private SettingsService(Package package) {
      _package = package;
    }

    public event EventHandler<SettingsService>? SettingsChanged;

    public void ShitChangedYo() {
      SettingsChanged?.Invoke(this, this);
    }

    public bool DrawSticker {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.DrawSticker;
      }
    }

    public bool DrawWallpaper {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.DrawWallpaper;
      }
    }

    public string CustomStickerImageAbsolutePath {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.CustomStickerImageAbsolutePath;
      }
    }

    public string CustomWallpaperImageAbsolutePath {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.CustomWallpaperImageAbsolutePath;
      }
    }

    public double WallpaperOpacity {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.WallpaperOpacity;
      }
    }

    public double WallpaperOffsetX {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.WallpaperOffsetX;
      }
    }


    public double WallpaperOffsetY {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.WallpaperOffsetY;
      }
    }

    public Stretch WallpaperFill {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return BackgroundSizeConverter.ConvertTo(page.WallpaperFill);
      }
    }


    public BackgroundAnchor WallpaperAnchor {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.WallpaperAnchor;
      }
    }
  }

  [ComVisible(true)]
  [Guid("C89AFB79-39AF-4716-BB91-6977323DD89B")]
  public enum BackgroundSize {
    Filled = 1,
    Scaled = 2
  }

  [ComVisible(true)]
  [Guid("C69AFB79-39AF-4716-BB91-6977323DD89B")]
  public enum BackgroundAnchor {
    Default = 1,
    Left = 2,
    Center = 3,
    Right = 4
  }

  public static class BackgroundSizeConverter{
    public static Stretch ConvertTo(this BackgroundSize backgroundSize) {
      switch (backgroundSize) {
        case BackgroundSize.Filled:
          return Stretch.UniformToFill;
        case BackgroundSize.Scaled:
          return Stretch.Uniform;
      }
      return Stretch.Uniform;
    }
  }

  class DokiThemeSettings : DialogPage {
    bool _drawSticker = true;

    [DescriptionAttribute("Draw the cute sticker in the bottom right hand corner of your editor?")]
    public bool DrawSticker {
      get { return _drawSticker; }
      set { _drawSticker = value; }
    }

    bool _drawWallpaper = true;

    [DescriptionAttribute("Draw the beautiful wallpaper in the background of your editor?")]
    public bool DrawWallpaper {
      get { return _drawWallpaper; }
      set { _drawWallpaper = value; }
    }

    [DescriptionAttribute("Use custom image for wallpaper, to use default image clear the value from this setting")]
    [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
    public string CustomWallpaperImageAbsolutePath { get; set; }

    [DescriptionAttribute("Use custom image for sticker, to use default image clear the value from this setting")]
    [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
    public string CustomStickerImageAbsolutePath { get; set; }

    [DescriptionAttribute("Double value in the range of [-1.0,1] that skews the wallpaper right (eg: -0.25) or left (0.25)")]
    public double WallpaperOffsetX { get; set; }

    [DescriptionAttribute("Double value in the range of [-1.0,1] that skews the wallpaper down (eg: -0.25) or up (eg: 0.25)")]
    public double WallpaperOffsetY { get; set; }

    private double _wallpaperOpacity = -1.0;

    [DescriptionAttribute("Customize the wallpaper opacity, set to -1.0 to use default value for theme")]
    [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
    public double WallpaperOpacity {
      get { return _wallpaperOpacity; }
      set { _wallpaperOpacity = value; }
    }

    private BackgroundSize _wallpaperFill = BackgroundSize.Filled;

    [DescriptionAttribute("Choose how the wallpaper gets painted in the background")]
    public BackgroundSize WallpaperFill{
      get { return _wallpaperFill; }
      set { _wallpaperFill = value; }
    }

    private BackgroundAnchor _wallpaperAnchor = BackgroundAnchor.Default;

    [DescriptionAttribute("Choose how the wallpaper gets anchored in the background")]
    public BackgroundAnchor WallpaperAnchor
    {
      get { return _wallpaperAnchor; }
      set { _wallpaperAnchor = value; }
    }


    protected override void OnApply(PageApplyEventArgs e) {
      base.OnApply(e);
      SettingsService.Instance.ShitChangedYo();
    }
  }

  internal class BrowseFile : UITypeEditor {
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
      return UITypeEditorEditStyle.Modal;
    }

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
      IWindowsFormsEditorService edSvc =
        (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
      if (edSvc != null) {
        OpenFileDialog open = new OpenFileDialog();
        open.FileName = Path.GetFileName((string)value);

        try {
          open.InitialDirectory = Path.GetDirectoryName((string)value);
        } catch (Exception) {
        }

        if (open.ShowDialog() == DialogResult.OK) {
          return open.FileName;
        }
      }

      return value;
    }

    public override bool GetPaintValueSupported(ITypeDescriptorContext context) {
      return false;
    }
  }
}
