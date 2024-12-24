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

    }

}