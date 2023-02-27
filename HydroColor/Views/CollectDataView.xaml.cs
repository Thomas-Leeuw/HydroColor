using HydroColor.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Maps;

namespace HydroColor.Views;

public partial class CollectDataView : ContentPage
{
	public CollectDataView(CollectDataViewModel collectDataViewModel)
	{
		InitializeComponent();
        Action<MapSpan> MoveMapLocationAction = new Action<MapSpan>((span) => {
            UserLocationMap.MoveToRegion(span);
            });

        collectDataViewModel.MoveMapLocationAction = MoveMapLocationAction;
        BindingContext = collectDataViewModel;

        // The popup displayed by DisplayPromptAysnc on Android pushes the controls up instead of overlaying ontop of them
        // If there isn't room for the controls to be pushed out the way, it causes a huge memory leak. Seems to be a bug in .NET MAUI :(
        // The only solution was to wrap everything in a scroll view, presumably this allows the controls to be pushed up and out of the way
        // However, the scrollview expands outside horizontal boundries of the screen. So the with of the grid that contains all the conrols
        // must be explicitly set.
        MainGrid.WidthRequest = Application.Current.MainPage.Width;

    }

  
}