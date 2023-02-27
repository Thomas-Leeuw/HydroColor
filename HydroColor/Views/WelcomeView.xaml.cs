using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class WelcomeView : ContentPage
{
	public WelcomeView(WelcomeViewModel welcomeViewModel)
	{
		InitializeComponent();

		BindingContext = welcomeViewModel;

	}
}