using Android.Hardware.Camera2;

namespace HydroColor.Platforms.Android.Callbacks
{
    public class CameraCaptureListener : CameraCaptureSession.CaptureCallback
    {
        public Action<CameraCaptureSession, CaptureRequest, long, long> OnCaptureStartedAction;
        public Action<CameraCaptureSession, CaptureRequest, TotalCaptureResult> OnCaptureCompletedAction;
        public Action<CameraCaptureSession, CaptureRequest, CaptureFailure> OnCaptureFailedAction;
        public Action<CameraCaptureSession, int, long> OnCaptureSequenceCompletedAction;

        public override void OnCaptureStarted(CameraCaptureSession session, CaptureRequest request, long timestamp, long frameNumber)
            => OnCaptureStartedAction(session, request, timestamp, frameNumber);

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            => OnCaptureCompletedAction(session, request, result);

        public override void OnCaptureFailed(CameraCaptureSession session, CaptureRequest request, CaptureFailure failure)
            => OnCaptureFailedAction(session, request, failure);

        public override void OnCaptureSequenceCompleted(CameraCaptureSession session, int sequenceId, long frameNumber)
            => OnCaptureSequenceCompletedAction(session, sequenceId, frameNumber);
    }
}
