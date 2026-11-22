
namespace HydroColor.Models
{
    public class ImageCaptureEventArgs : EventArgs
    {
        // The captured image, or null if the capture failed
        public HydroColorRawImageData ImageData { get; set; }

        // Set when the capture failed
        public string ErrorMessage { get; set; }
    }
}
