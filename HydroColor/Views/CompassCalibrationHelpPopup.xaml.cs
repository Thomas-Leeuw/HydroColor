using CommunityToolkit.Maui.Views;
namespace HydroColor.Views;

public partial class CompassCalibrationHelpPopup : Popup
{
	public CompassCalibrationHelpPopup()
	{
        InitializeComponent();

        // required for popup to appear inside the bounds of the screen
        PopupFrame.WidthRequest = Shell.Current.CurrentPage.Width - 75;
        PopupFrame.HeightRequest = 400;

    }

    private void OKButton_Clicked(object sender, EventArgs e)
    {
		Close();
    }
}