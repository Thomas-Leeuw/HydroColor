using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class WelcomeView : ContentPage
{
	private Action PhysicalBackButtonPressed { get; set; }

	public WelcomeView(WelcomeViewModel welcomeViewModel)
	{
		InitializeComponent();
        PhysicalBackButtonPressed = welcomeViewModel.BackButtonClick;
        BindingContext = welcomeViewModel;

	}

    protected override bool OnBackButtonPressed()
    {
		PhysicalBackButtonPressed?.Invoke();
        return true;
    }
}