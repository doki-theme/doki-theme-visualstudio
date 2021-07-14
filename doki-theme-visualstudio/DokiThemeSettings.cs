using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace doki_theme_visualstudio {
  public class SettingsService {
    private static SettingsService? _instance;

    public static SettingsService Instance =>
      _instance ?? throw new Exception("Expected settings to be initialized!");

    public static void Init(Package package) {
      _instance ??= new SettingsService(package);
    }

    private readonly Package _package;

    private SettingsService(Package package) {
      _package = package;
    }

    public bool Bustin {
      get {
        var page = (DokiThemeSettings)_package.GetDialogPage(typeof(DokiThemeSettings));
        return page.Bustin;
      }
    }
  }


  class DokiThemeSettings : DialogPage {
    bool _bustin = true;

    [DescriptionAttribute("Bustin makes me feel good")]
    public bool Bustin {
      get { return _bustin; }
      set { _bustin = value; }
    }

    [DescriptionAttribute("Bustin makes me feel good")]
    [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
    public string WallpaperImageAbsolutePath { get; set; }

    protected override void OnApply(PageApplyEventArgs e) {
      base.OnApply(e);
      Bustin.Equals(e);
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
