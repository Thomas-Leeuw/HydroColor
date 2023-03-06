using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
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
                    IntroTitle = "Welcome to HydroColor",
                    IntroBody = "HydroColor is a tool for measuring the reflectance (or simply color) of natural water bodies. The measured reflectance can be used to infer various properties of the water. \n\nHydroColor provides reflectance measurements in the red, green, and blue color channels of the camera. Additionally, water turbidity is derived from the reflectance in the red channel.",
                    IntroImage = "appicon_image.png"
                },
                new()
                {
                    IntroTitle = "What’s Needed?",
                    IntroBody = "HydroColor requires a photographers 18% reflectance gray card. These are fairly standardized and only cost a few dollars. However, you must ensure the card is specified as 18% reflectance. \n\nYou will need access to deep unshaded water. If the bottom is visible, it is too shallow to use HydroColor. Docks and piers are usually the best place to perform HydroColor measurements.",
                    IntroImage = "graycard.png"
                },
                new()
                {
                    IntroTitle = "Collecting Images",
                    IntroBody = "HydroColor requires the collection of a gray card, water, and sky image to compute the reflectance. HydroColor uses the center of each image, show by a square over the camera preview. Ensure the area inside the square is uniformly filled with the gray card, sky, or water.\n\nThe gray card must be held level or placed on level surface before taking the gray card picture. Avoid imaging shadows on the gray card or debris on the water surface.",
                    IntroImage = "image_set.png"
                },
                new()
                {
                    IntroTitle = "Image Angles",
                    IntroBody = "Each image is taken at a specific angle to minimize surface reflection.\n\nA compass and tilt display will guide you to the correct image angles. Line up the green arrows on both displays before capturing an image.",
                    IntroImage = "image_angle_guides.png"
                },
                new()
                {
                    IntroTitle = "Learn More",
                    IntroBody = "You can learn more about HydroColor at the following links:",
                    IntroImage = "learn_more.png"
                }
            };

            HideWelcomeScreenChecked = Preferences.Default.Get(PreferenceKeys.HideWelcomeScreen, false);

            CarouselPositionChanged();
        }

        [RelayCommand]
        async void NextButtonClick()
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
        void BackButtonClick()
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
            NextButtonText = CarouselPosition == WelcomePages.Count - 1 ? "Start" : "Next";
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
