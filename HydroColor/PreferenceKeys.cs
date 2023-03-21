
namespace HydroColor
{
    public static class PreferenceKeys
    {
        public const string HideWelcomeScreen = "HideWelcomeScreen";        // key to preferences variable that remembers if the user opted to hide the welcome screen
        public const string SavedEmailAddresses = "SavedEmailAddresses"; // key to preferences variable where email address are stored
        public const string UserEnteredDataFileSuffix = "UserDataFileSuffix"; // key to remember the last custom data file name the user entered (when emailing data file)
        public const string DatafileUpdatedToV2p1 = "v2p1DataFileUpdated";    // key for checking if the version number has been added to the data file (added in v2.1)
    }
}
