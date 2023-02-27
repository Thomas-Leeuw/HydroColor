using HydroColor.Models;

namespace HydroColor.ViewModels
{
    public class CameraPreview : View
    {
        public event EventHandler ImageCaptureRequested;
        public event EventHandler CameraDidNotOpen;

        public void CaptureImage()
        {
            ImageCaptureRequested?.Invoke(this, EventArgs.Empty);
        }

        public static readonly BindableProperty CapturedImageDataProperty = BindableProperty.Create(
        propertyName: nameof(CapturedImageData),
        returnType: typeof(HydroColorRawImageData),
        declaringType: typeof(CameraPreview),
        defaultValue: null);

        public HydroColorRawImageData CapturedImageData
        {
            get { return (HydroColorRawImageData)GetValue(CapturedImageDataProperty); }
            set { SetValue(CapturedImageDataProperty, value); }
        }

        public void CameraFailedToOpen()
        {
            CameraDidNotOpen(this,EventArgs.Empty);
        }
    }
}
