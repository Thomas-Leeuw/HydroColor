using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class CaptureImageView : ContentPage
{

    public CaptureImageView(CaptureImageViewModel captureImageViewModel)
	{
		InitializeComponent();
        BindingContext = captureImageViewModel;
    }

    // Gives the camera picture the largest 3 wide by 4 tall size that fits in the space available,
    // so the camera picture fills it exactly, and leaves room below it for the compass and Capture button.
    private void PreviewArea_SizeChanged(object sender, EventArgs e)
    {
        double availableWidth = PreviewArea.Width;
        double availableHeight = PreviewArea.Height;
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        double width = Math.Min(availableWidth, availableHeight * 3.0 / 4.0);
        double height = width * 4.0 / 3.0;

        // The compass and Capture button need this much room below the picture. On a short screen the
        // bottom of the picture is hidden to make that room, but never so much that the measured square
        // (which ends at half the height plus a quarter of the width) is covered.
        const double controlsHeight = 220;
        double visibleHeight = Math.Min(height, availableHeight - controlsHeight);
        visibleHeight = Math.Max(visibleHeight, height / 2 + width / 4 + 10);

        // The frame is the visible window; the picture inside it (preview, dimming layer, tilt indicator,
        // measured square) keeps its full size, so the square stays at the center of the camera picture.
        PictureFrame.WidthRequest = width;
        PictureFrame.HeightRequest = visibleHeight;
        PictureContent.HeightRequest = height;

        // The tilt indicator stops at the left edge of the measured square (a quarter of the picture's
        // width in from the left), and is never wider than 100.
        TiltStrip.WidthRequest = Math.Min(100, width / 4);

        // The dimming layer: the whole picture, with a hole where the measured square is
        // (half the picture's width, centered, with the same rounded corners as the square's border).
        double square = width / 2;
        var dimShape = new Microsoft.Maui.Controls.Shapes.GeometryGroup { FillRule = Microsoft.Maui.Controls.Shapes.FillRule.EvenOdd };
        dimShape.Children.Add(new Microsoft.Maui.Controls.Shapes.RectangleGeometry(new Rect(0, 0, width, height)));
        dimShape.Children.Add(new Microsoft.Maui.Controls.Shapes.RoundRectangleGeometry(new CornerRadius(20), new Rect((width - square) / 2, (height - square) / 2, square, square)));
        DimOverlay.Data = dimShape;
    }

    private void CaptureImageContentPage_Unloaded(object sender, EventArgs e)
    {
        cameraPreview.Handler?.DisconnectHandler();
    }
}