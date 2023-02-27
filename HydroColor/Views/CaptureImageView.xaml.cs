using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class CaptureImageView : ContentPage
{

    public CaptureImageView(CaptureImageViewModel captureImageViewModel)
	{
		InitializeComponent();
        Action CaptureImageAction = new Action(() => cameraPreview.CaptureImage());
        captureImageViewModel.CaptureImage = CaptureImageAction;
        BindingContext = captureImageViewModel;
    }
}