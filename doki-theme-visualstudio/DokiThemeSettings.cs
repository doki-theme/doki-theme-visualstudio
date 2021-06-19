using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace doki_theme_visualstudio {
  class DokiThemeSettings : DialogPage {
    bool helloWorld = true;

    public bool HelloWorld {
      get { return helloWorld; }
      set { helloWorld = value; }
    }

    [DescriptionAttribute("Bustin makes me feel good")]
    [EditorAttribute(typeof(BrowseFile), typeof(UITypeEditor))]
    public string WallpaperImageAbsolutePath { get; set; }
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
        }
        catch (Exception) {
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
