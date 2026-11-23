namespace HydroColor.Models
{
    // The ISO and exposure time range a camera accepts when they are set by the app (sensitivity test only)
    public class ManualExposureLimits
    {
        public bool Supported { get; set; }
        public double MinISO { get; set; }
        public double MaxISO { get; set; }
        public double MinExposureSeconds { get; set; }
        public double MaxExposureSeconds { get; set; }

        // Highest ISO reached with analog gain alone, when the phone reports it (Android)
        public double? MaxAnalogISO { get; set; }
    }
}
