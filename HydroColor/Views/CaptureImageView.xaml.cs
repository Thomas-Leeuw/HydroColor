using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class CaptureImageView : ContentPage
{

    public CaptureImageView(CaptureImageViewModel captureImageViewModel)
	{
		InitializeComponent();
        BindingContext = captureImageViewModel;
    }

    private void CaptureImageContentPage_Unloaded(object sender, EventArgs e)
    {
        cameraPreview.Handler?.DisconnectHandler();
    }
}