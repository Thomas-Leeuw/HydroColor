using HydroColor.Views;

namespace HydroColor;

public partial class App : Application
{
    public App()
	{
		InitializeComponent();

        MainPage = new AppShell();
    }

}
