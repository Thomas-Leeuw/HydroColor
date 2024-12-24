using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
using HydroColor.Resources.Strings;
using System.Collections.ObjectModel;

namespace HydroColor.ViewModels
{
    public partial class WelcomeViewModel : ObservableObject
    {

        [ObservableProperty]
        int carouselPosition;

        [ObservableProperty]
        ObservableCollection<WelcomePageModel> welcomePages;

        [ObservableProperty]
        bool backButtonVisible;

        [ObservableProperty]
        string nextButtonText;

        [ObservableProperty]
        bool hideWelcomeScreenChecked;

        [ObservableProperty]
        bool learnMoreLinksVisible;

        public WelcomeViewModel()
        {

            WelcomePages = new ObservableCollection<WelcomePageModel>
            {
                new()
                {
                    IntroTitle = Strings.Welcome_WelcomeToHydroColorTitle,
                    IntroBody = Strings.Welcome_WelcomeToHydroColorBody,
                    IntroImage = "appicon_image.png"
                },
                new()
                {
                    IntroTitle = Strings.Welcome_WhatsNeededTitle,
                    IntroBody = Strings.Welcome_WhatsNeededBody,
                    IntroImage = "graycard.png"
                },
                new()
                {
                    IntroTitle = Strings.Welcome_CollectingImagesTitle,
                    IntroBody = Strings.Welcome_CollectingImagesBody,
                    IntroImage = "image_set.png"
                },
                new()
                {
                    IntroTitle = Strings.Welcome_ImageAnglesTitle,
                    IntroBody = Strings.Welcome_ImageAnglesBody,
                    IntroImage = "image_angle_guides.png"
                },
                new()
                {
                    IntroTitle = Strings.Welcome_LearnMoreTitle,
                    IntroBody = Strings.Welcome_LearnMoreBody,
                    IntroImage = "learn_more.png"
                }
            };

            HideWelcomeScreenChecked = Preferences.Default.Get(PreferenceKeys.HideWelcomeScreen, false);

            CarouselPositionChanged();
        }

        [RelayCommand]
        async Task NextButtonClick()
        {
            if (CarouselPosition + 1 < WelcomePages.Count)
            {
                int initialPosition = CarouselPosition;
                CarouselPosition++;               
            }
            else
            {
                Preferences.Default.Set(PreferenceKeys.HideWelcomeScreen, HideWelcomeScreenChecked);
                await Shell.Current.GoToAsync("//MainTabView");
            }
        }

        [RelayCommand]
        public void BackButtonClick()
        {
            if (CarouselPosition - 1 >= 0)
            {
                CarouselPosition--;
            }
        }

        [RelayCommand]
        void CarouselPositionChanged()
        {
            BackButtonVisible = CarouselPosition != 0;
            NextButtonText = CarouselPosition == WelcomePages.Count - 1 ? Strings.Welcome_StartButton : Strings.Welcome_NextButton;
            LearnMoreLinksVisible = CarouselPosition == 4;

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
