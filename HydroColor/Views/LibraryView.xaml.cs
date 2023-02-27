using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class LibraryView : ContentPage
{
	public LibraryView(LibraryViewModel libraryViewModel)
	{
		InitializeComponent();
		BindingContext = libraryViewModel;
	}
}