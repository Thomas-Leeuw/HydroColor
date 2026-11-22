
namespace HydroColor.Models
{
    public class WelcomePageModel
    {
        public ImageSource IntroImage { get; set; }
        public string IntroTitle { get; set; }
        public string IntroBody { get; set; }

        // True only for the page that shows the "learn more" links.
        // Fixed per page so the carousel items never change size while scrolling.
        public bool ShowLearnMoreLinks { get; set; }
    }
}
