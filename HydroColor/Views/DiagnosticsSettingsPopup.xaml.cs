using CommunityToolkit.Maui.Views;
namespace HydroColor.Views;

public partial class DiagnosticsSettingsPopup : Popup
{
    public DiagnosticsSettingsPopup()
    {
        InitializeComponent();

        // required for popup to appear inside the bounds of the screen
        PopupFrame.WidthRequest = Shell.Current.CurrentPage.Width - 75;
    }

    private void CloseButton_Clicked(object sender, EventArgs e)
    {
        CloseAsync();
    }

    // Closes the popup, then opens the sensitivity test page
    private async void SensitivityTestButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await CloseAsync();
            await Shell.Current.GoToAsync(nameof(SensitivityTestView));
        }
        catch
        {
        }
    }
}
