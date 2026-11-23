using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class SensitivityTestView : ContentPage
{
    public SensitivityTestView(SensitivityTestViewModel sensitivityTestViewModel)
    {
        InitializeComponent();
        BindingContext = sensitivityTestViewModel;
    }

    private void SensitivityTestContentPage_Unloaded(object sender, EventArgs e)
    {
        cameraPreview.Handler?.DisconnectHandler();
    }
}
