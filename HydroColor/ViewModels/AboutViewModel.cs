using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Services;
using HydroColor.Views;

namespace HydroColor.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {

        [ObservableProperty]
        string appVersionNumber = AppInfo.Current.VersionString;

        [RelayCommand]
        void ShowWelcomeScreen()
        {
            Shell.Current.GoToAsync(nameof(WelcomeView));
        }


        // Opens the camera diagnostics popup (button at the bottom of the About page)
        [RelayCommand]
        void ShowDiagnosticsSettings()
        {
            var popup = new DiagnosticsSettingsPopup();
            popup.BindingContext = this;

            Shell.Current.CurrentPage.ShowPopupAsync(popup);
        }

        // The "Camera diagnostics" switch in that popup (also shows its Sensitivity Test button).
        // Kept in memory only, so it is off every time the app starts.
        [ObservableProperty]
        bool diagnosticsOn;

        partial void OnDiagnosticsOnChanged(bool value)
        {
            CameraDiagnostics.Enabled = value;
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
