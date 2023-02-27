
namespace HydroColor.Models
{
    public class HydroColorImageCaptureModel
    {
        // Auxillary Data
        public double ImageOffNadirAngle { get; set; }
        public double ImageAzimuthAngle { get; set; }
        public bool ImageCapturedAtCorrectAngles { get; set; }
        public double SunAzimuthAngle { get; set; }
        public double SunElevationAngle { get; set; }
        public Location ImageLocation { get; set; } = new();
        public double MagneticDeclination { get; set; }
        public DateTime LocalTimeStamp { get; set; }
        public double UTCOffset { get; set; }

        // Camera Data
        public ColorChannelData<double> MedianPixelValue { get; set; } = new();
        public double ExposureTime { get; set; }
        public double SensorSensitivity { get; set; }
        public BayerFilterType BayerFilterPattern { get; set; }
        public ColorChannelData<double> BlackLevel { get; set; } = new();
        public byte[] JpegImage { get; set; }


    }
}
