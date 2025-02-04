using Microsoft.Maui.Handlers;
using HydroColor.Controls;

#if ANDROID
using NativeCameraView = HydroColor.Platforms.Android.NativeCameraView;
#elif IOS
using NativeCameraView = HydroColor.Platforms.iOS.NativeCameraView;
#endif

namespace HydroColor.Handlers
{

    // This handler class provides the link between the cross platform 'CameraPreview'
    // and the platform specific 'NativeCameraView' and 'CameraController' classes
    // The 'CameraPreview' has a 'CameraController' properity. 

    public class CameraPreviewHandler : ViewHandler<CameraPreview, NativeCameraView>
    {
        public static IPropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper = new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewMapper)
        {
            // add future properties here
        };

        public static CommandMapper<CameraPreview, CameraPreviewHandler> CommandMapper = new(ViewCommandMapper)
        {
            // add future commands here
        };

        public CameraPreviewHandler() : base(PropertyMapper, CommandMapper)
        {
        }

        protected override NativeCameraView CreatePlatformView() => new NativeCameraView();

        protected override void ConnectHandler(NativeCameraView platformView)
        {
            base.ConnectHandler(platformView);

            // The camera controller needs a UI element to display the frames.
            // However the dimensions are only correct after the view has been
            // fully laid out. So the view information is passed to the camera controller
            // in this custom LayoutFinished event.
            platformView.LayoutFinishedEvent += ((object sender, EventArgs e) =>
            {
#if ANDROID
                VirtualView.CameraControl.SetTextureView(platformView.textureView);
#elif IOS
                VirtualView.CameraControl.SetCALayer(platformView.Layer);
#endif
                VirtualView.LayoutFinished = true; // set layout finished property on the 'CameraPreview' control
            });
        }

        protected override void DisconnectHandler(NativeCameraView platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

    }
}
