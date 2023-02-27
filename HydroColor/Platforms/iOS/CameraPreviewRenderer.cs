using HydroColor.Models;
using HydroColor.ViewModels;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform;
using UIKit;

namespace HydroColor.Platforms.iOS
{
    public class CameraPreviewRenderer : ViewRenderer<CameraPreview, UIView>
    {
        CameraController mCameraController;
        CameraPreview mCameraPreview;
        bool CameraOpened;

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                e.OldElement.ImageCaptureRequested -= OnImageCaptureRequested;
                mCameraController = null;
                mCameraPreview = null;
            }
            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    mCameraPreview = e.NewElement;
                    mCameraController = new CameraController();
                    mCameraController.ImageCaptureCompletedEvent += OnImageCaptureCompleted;
                    SetNativeControl(this.Control);
                }
                
                e.NewElement.ImageCaptureRequested += OnImageCaptureRequested;
            }
        }

        public override async void LayoutSubviews()
        {
            base.LayoutSubviews();

            mCameraController.UILayer = Layer;
            mCameraController.UIBounds = Bounds;

            // LayoutSubviews is getting called multiple types
            // OpenCamera should only be called once
            // Need to find a better place to put OpenCamera...
            if (!CameraOpened)
            {
                CameraOpened = true;
                if (!await mCameraController.OpenCamera())
                {
                    mCameraPreview.CameraFailedToOpen();
                }
            }
            
        }

        void OnImageCaptureCompleted(object sender, ImageCaptureEventArgs e)
        {
            mCameraPreview.CapturedImageData = e.ImageData;
        }

        void OnImageCaptureRequested(object sender, EventArgs e)
        {
            mCameraController.TakeRAWImage();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mCameraController.CloseCamera();
                Control.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
