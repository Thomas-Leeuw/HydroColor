
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
        // CameraId, the raw image size and the Bayer pattern are used to check that the
        // gray card, sky and water images all came from the same camera
        public string CameraId { get; set; }
        public int RawImageWidth { get; set; }
        public int RawImageHeight { get; set; }
        public ColorChannelData<double> MedianPixelValue { get; set; } = new();
        public double ExposureTime { get; set; }
        public double SensorSensitivity { get; set; }
        public BayerFilterType BayerFilterPattern { get; set; }
        public ColorChannelData<double> BlackLevel { get; set; } = new();
        public byte[] JpegImage { get; set; }

        // Camera diagnostics for this image; null unless diagnostics mode is on
        public HydroColor.Services.CaptureDiagnostics Diagnostics { get; set; }


    }
}
