using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;


namespace doki_theme_visualstudio {
  /// <summary>
  ///   Establishes an <see cref="IAdornmentLayer" /> to place the adornment on and exports the
  ///   <see cref="IWpfTextViewCreationListener" />
  ///   that instantiates the adornment on the event of a <see cref="IWpfTextView" />'s creation
  /// </summary>
  [Export(typeof(IWpfTextViewCreationListener))]
  [ContentType("text")]
  [ContentType("BuildOutput")]
  [TextViewRole(PredefinedTextViewRoles.Document)]
  internal sealed class StickerAdornmentTextViewCreationListener : IWpfTextViewCreationListener {
    // Disable "Field is never assigned to..." and "Field is never used" compiler's warnings. Justification: the field is used by MEF.
#pragma warning disable 649, 169

    /// <summary>
    ///   Defines the adornment layer for the scarlet adornment. This layer is ordered
    ///   after the selection layer in the Z-order
    /// </summary>
    [Export(typeof(AdornmentLayerDefinition))]
    [Name("StickerAdornment")]
    [Order(After = PredefinedAdornmentLayers.Caret)]
    private AdornmentLayerDefinition editorAdornmentLayer;

#pragma warning restore 649, 169

    /// <summary>
    ///   Instantiates a StickerAnornment manager when a textView is created.
    /// </summary>
    /// <param name="textView">The <see cref="IWpfTextView" /> upon which the adornment should be placed</param>
    public void TextViewCreated(IWpfTextView textView) {
      // The adornment will get wired to the text view events
      if (SettingsService.IsInitialized()) {
        new StickerAdornment(textView);
      } else {
        Task.Run(async () => {
          await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
          new StickerAdornment(textView);
        }).FileAndForget("dokiTheme/stickerLoad");
      }
    }
  }
}
