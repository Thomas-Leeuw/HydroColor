using CommunityToolkit.Maui.Views;
namespace HydroColor.Views;

public partial class SendingEmailPopup : Popup
{
	public SendingEmailPopup()
	{
        InitializeComponent();

        // required for popup to appear inside the bounds of the screen
        PopupFrame.WidthRequest = Shell.Current.CurrentPage.Width - 125;
        PopupFrame.HeightRequest = 125;

    }


}