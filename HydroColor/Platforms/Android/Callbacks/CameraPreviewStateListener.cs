using Android.Hardware.Camera2;

namespace HydroColor.Platforms.Android.Callbacks
{
    public class CameraPreviewStateListener : CameraCaptureSession.StateCallback
    {
        public Action<CameraCaptureSession> OnConfigureFailedAction;
        public Action<CameraCaptureSession> OnConfiguredAction;
        public Action<CameraCaptureSession> OnClosedAction;

        public override void OnConfigureFailed(CameraCaptureSession session) => OnConfigureFailedAction?.Invoke(session);
        public override void OnConfigured(CameraCaptureSession session) => OnConfiguredAction?.Invoke(session);
        public override void OnClosed(CameraCaptureSession session) => OnClosedAction?.Invoke(session);
    }
}
