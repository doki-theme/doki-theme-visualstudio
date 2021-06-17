using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace doki_theme_visualstudio
{
  /// <summary>
  /// Adornment class that draws a square box in the top right hand corner of the viewport
  /// </summary>
  internal sealed class ViewportAdornment1
  {
    /// <summary>
    /// The width of the square box.
    /// </summary>
    private const double AdornmentWidth = 30;

    /// <summary>
    /// The height of the square box.
    /// </summary>
    private const double AdornmentHeight = 30;

    /// <summary>
    /// Distance from the viewport top to the top of the square box.
    /// </summary>
    private const double TopMargin = 30;

    /// <summary>
    /// Distance from the viewport right to the right end of the square box.
    /// </summary>
    private const double RightMargin = 30;

    /// <summary>
    /// Text view to add the adornment on.
    /// </summary>
    private readonly IWpfTextView view;

    /// <summary>
    /// Adornment image
    /// </summary>
    private readonly Image image;

    /// <summary>
    /// The layer for the adornment.
    /// </summary>
    private readonly IAdornmentLayer adornmentLayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewportAdornment1"/> class.
    /// Creates a square image and attaches an event handler to the layout changed event that
    /// adds the the square in the upper right-hand corner of the TextView via the adornment layer
    /// </summary>
    /// <param name="view">The <see cref="IWpfTextView"/> upon which the adornment will be drawn</param>
    public ViewportAdornment1(IWpfTextView view)
    {
      if (view == null)
      {
        throw new ArgumentNullException("view");
      }

      this.view = view;

      var brush = new SolidColorBrush(Colors.BlueViolet);
      brush.Freeze();
      var penBrush = new SolidColorBrush(Colors.Red);
      penBrush.Freeze();
      var pen = new Pen(penBrush, 0.5);
      pen.Freeze();

      // Draw a square with the created brush and pen
      System.Windows.Rect r = new System.Windows.Rect(0, 0, AdornmentWidth, AdornmentHeight);
      var geometry = new RectangleGeometry(r);

      var drawing = new GeometryDrawing(brush, pen, geometry);
      drawing.Freeze();

      var drawingImage = new DrawingImage(drawing);
      drawingImage.Freeze();

      this.image = new Image
      {
        Source = drawingImage,
      };

      this.adornmentLayer = view.GetAdornmentLayer("ViewportAdornment1");

      this.view.LayoutChanged += this.OnSizeChanged;
    }

    /// <summary>
    /// Event handler for viewport layout changed event. Adds adornment at the top right corner of the viewport.
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments</param>
    private void OnSizeChanged(object sender, EventArgs e)
    {
      // Clear the adornment layer of previous adornments
      this.adornmentLayer.RemoveAllAdornments();

      // Place the image in the top right hand corner of the Viewport
      Canvas.SetLeft(this.image, this.view.ViewportRight - RightMargin - AdornmentWidth);
      Canvas.SetTop(this.image, this.view.ViewportTop + TopMargin);

      // Add the image to the adornment layer and make it relative to the viewport
      this.adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this.image, null);
    }
  }
}
