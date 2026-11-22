
namespace HydroColor
{
    // TEMPLATE - placeholder values only. This file is committed to git.
    //
    // To use real keys, copy this file to 'Secrets.cs' (in the same folder) and fill in your own values.
    // 'Secrets.cs' is listed in .gitignore, so it is never committed.
    // When 'Secrets.cs' exists, the project file excludes this template from the build.
    //
    // Without real keys the app still builds and runs, but:
    //   - the map on the Collect Data tab is blank (Android)
    //   - emailing the data file shows the "Email Failed" message
    public static class Secrets
    {
        // Google Maps SDK for Android key (Google Cloud Console)
        public const string GoogleMapsApiKey = "YOUR_GOOGLE_MAPS_API_KEY";

        // Account used to send the data file by SMTP
        public const string EmailAddress = "sender@example.com";
        public const string EmailUserName = "sender@example.com";
        public const string EmailPassword = "YOUR_EMAIL_APP_PASSWORD";
        public const string EmailHost = "smtp.example.com";
    }
}
