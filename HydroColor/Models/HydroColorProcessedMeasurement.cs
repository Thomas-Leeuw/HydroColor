
namespace HydroColor.Models
{
    public partial class HydroColorProcessedMeasurement 
    {
        public HydroColorImageCaptureModel GrayCardImageData { get; set; } = new();
        public HydroColorImageCaptureModel SkyImageData { get; set; } = new();
        public HydroColorImageCaptureModel WaterImageData { get; set; } = new();

        public string MeasurementName { get; set; }

        public HydroColorMeasurementProducts MeasurementProducts { get; set; } = new();

        public string DeviceManufacturer { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceOSVersion { get; set; }
        public string HydroColorVersion { get; set; }

    }
}
