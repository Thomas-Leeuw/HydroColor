using AVFoundation;
using Foundation;

namespace HydroColor.Platforms.iOS
{
    public class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
    {
        public Action<AVCapturePhotoOutput, AVCapturePhoto, NSError> DidFinishProcessingPhotoAction;
        public Action<AVCapturePhotoOutput, AVCaptureResolvedPhotoSettings> DidCapturePhotoAction;
        public Action<AVCapturePhotoOutput, AVCaptureResolvedPhotoSettings> WillCapturePhotoAction;


        public override void DidFinishProcessingPhoto(AVCapturePhotoOutput captureOutput, AVCapturePhoto photo, NSError error)
            => DidFinishProcessingPhotoAction(captureOutput, photo, error);

        public override void DidCapturePhoto(AVCapturePhotoOutput captureOutput, AVCaptureResolvedPhotoSettings resolvedSettings)
            => DidCapturePhotoAction(captureOutput, resolvedSettings);

        public override void WillCapturePhoto(AVCapturePhotoOutput captureOutput, AVCaptureResolvedPhotoSettings resolvedSettings)
            => WillCapturePhotoAction(captureOutput, resolvedSettings);

    }
}
