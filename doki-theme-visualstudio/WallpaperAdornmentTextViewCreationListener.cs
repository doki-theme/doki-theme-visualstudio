using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;


namespace doki_theme_visualstudio {
  /// <summary>
  /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
  /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
  /// </summary>
  [Export(typeof(IWpfTextViewCreationListener))]
  [ContentType("text")]
  [TextViewRole(PredefinedTextViewRoles.Document)]
  internal sealed class WallpaperAdornmentTextViewCreationListener : IWpfTextViewCreationListener {
    // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169

    /// <summary>
    /// Defines the adornment layer for the scarlet adornment. This layer is ordered
    /// after the selection layer in the Z-order
    /// </summary>
    [Export(typeof(AdornmentLayerDefinition))]
    [Name("WallpaperAdornment")]
    [Order(After = PredefinedAdornmentLayers.Caret)]
    // [Order(Before = PredefinedAdornmentLayers.DifferenceChanges)]
    private AdornmentLayerDefinition editorAdornmentLayer;

#pragma warning restore 649, 169

    /// <summary>
    /// Instantiates a WallpaperAdornment manager when a textView is created.
    /// </summary>
    /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
    public void TextViewCreated(IWpfTextView textView) {
      if (TheDokiTheme.IsInitialized()) {
        new WallpaperAdornment(textView);
      } else {
        TheDokiTheme.PluginInitialized += (_, __) =>  {
          Task.Run(async () =>{
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            new WallpaperAdornment(textView);
          }).FileAndForget("dokiTheme/wallpaperLoad");
        };
      }
    }
  }
}
