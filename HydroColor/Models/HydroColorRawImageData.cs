
namespace HydroColor.Models
{
    public enum BayerFilterType
    {
        BGGR,
        GBRG,
        GRBG,
        RGGB,
        Unknown = -99
    }

    public class ColorChannelData<T>
    {
        public T Red { get; set; } 
        public T Green { get; set; } 
        public T Blue { get; set; } 
    }

    public class HydroColorRawImageData
    {
        public byte[] RawImagePixelData { get; set; }
        public int ImageHeight { get; set; }
        public int ImageWidth { get; set; }
        public int RowStride { get; set; }
        public double ExposureTime { get; set; }
        public double SensorSensitivity { get; set; }
        public BayerFilterType BayerFilterPattern { get; set; }
        public ColorChannelData<double> BlackLevel { get; set; } = new();
        public byte[] JpegImage { get; set; }
    }

}
