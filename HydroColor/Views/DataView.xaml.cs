using HydroColor.Models;
using HydroColor.ViewModels;

namespace HydroColor.Views;

public partial class DataView : ContentPage
{
	public DataView(DataViewModel dataViewModel)
	{
		InitializeComponent();
		BindingContext = dataViewModel;

        
    }

    protected override bool OnBackButtonPressed()
    {
        // Passing null for the Query Properties is a workaround for a known issue (https://github.com/dotnet/maui/issues/10294)
        // If null is not sent, it will resend the old data again, even if the local parameters have been set to null
        Shell.Current.GoToAsync($"..", true, new Dictionary<string, object>
        {
            [nameof(HydroColorImageTag.GrayCard)] = null,
            [nameof(HydroColorImageTag.Water)] = null,
            [nameof(HydroColorImageTag.Sky)] = null
        });

        return true;

    }


}