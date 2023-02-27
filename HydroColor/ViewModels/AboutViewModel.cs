using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Views;

namespace HydroColor.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {

        [RelayCommand]
        void ShowWelcomeScreen()
        {
            Shell.Current.GoToAsync(nameof(WelcomeView));
        }


        [RelayCommand]
        void LinkClicked(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch
            {

            }
        }

    }
}
