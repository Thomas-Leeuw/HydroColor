using Microsoft.Maui.Handlers;
using HydroColor.ViewModels;
#if ANDROID
using CustomCameraView = HydroColor.Platforms.Android.CustomCameraView;
#elif IOS
using CustomCameraView = HydroColor.Platforms.iOS.CustomCameraView;
#endif

namespace HydroColor.Handlers
{

    // This handler class provides the link between the cross platform 'CameraPreview'
    // and the platform specific 'CameraPreivewRenderer' and 'CameraController' classes
    // The 'CameraPreview' has a 'CameraController' properity. 

    public class CameraPreviewHandler : ViewHandler<CameraPreview, CustomCameraView>
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

        protected override CustomCameraView CreatePlatformView() => new CustomCameraView();

        protected override void ConnectHandler(CustomCameraView platformView)
        {
            base.ConnectHandler(platformView);

            // The camera controller need some information about the view dimensions.
            // However these dimensions are only correct after the view has been
            // fully laid out. So the view information is passed to the camera controller
            // in this custom LayoutFinished event.
            platformView.LayoutFinishedEvent += ((object sender, EventArgs e) =>
            {
#if ANDROID
                VirtualView.CameraControl.mTextureView = platformView.textureView;
                platformView.SetViewDimensions(VirtualView.CameraControl.GetRearFacingCameraCharacteristics());

#elif IOS
                VirtualView.CameraControl.UILayer = platformView.Layer;
                VirtualView.CameraControl.UIBounds = platformView.Bounds;
#endif
                VirtualView.LayoutFinished = true;
            });
        }

        protected override void DisconnectHandler(CustomCameraView platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

    }
}
