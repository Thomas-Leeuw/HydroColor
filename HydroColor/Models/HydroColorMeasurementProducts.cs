
namespace HydroColor.Models
{
    public class HydroColorMeasurementProducts
    {
        public ColorChannelData<double> GrayCardRelativeLightLevel { get; set; } = new();
        public ColorChannelData<double> SkyRelativeLightLevel { get; set; } = new();
        public ColorChannelData<double> WaterRelativeLightLevel { get; set; } = new();

        public ColorChannelData<double> Reflectance { get; set; } = new();

        public double WaterTurbidity { get; set; }
        public double SPM { get; set; }
        public double Backscatter_red { get; set; }



    }
}
