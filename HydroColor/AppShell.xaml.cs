using HydroColor.Views;

namespace HydroColor;

public partial class AppShell : Shell
{
	public AppShell()
	{
        InitializeComponent();

        Routing.RegisterRoute(nameof(CaptureImageView), typeof(CaptureImageView));
        Routing.RegisterRoute(nameof(DataView), typeof(DataView));
		Routing.RegisterRoute(nameof(WelcomeView), typeof(WelcomeView));

        if (Preferences.Default.Get(PreferenceKeys.HideWelcomeScreen, false))
        {
            GoToAsync("//MainTabView");
        }
        else
        {
            GoToAsync(nameof(WelcomeView));
        }

    }

}
