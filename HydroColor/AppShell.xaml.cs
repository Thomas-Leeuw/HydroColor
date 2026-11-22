using HydroColor.Views;
using System.Diagnostics;
using System.Globalization;
using System.Resources;

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
            CurrentItem = MainTabsShellContent; 
        }
        else
        {
            CurrentItem = WelcomeShellContent;  
        }

    }

}
