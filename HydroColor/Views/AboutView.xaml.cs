using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class AboutView : ContentPage
{
	public AboutView(AboutViewModel aboutViewModel)
	{
		InitializeComponent();
		BindingContext = aboutViewModel;
	}
}