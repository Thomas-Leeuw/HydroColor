using UIKit;

namespace HydroColor.Platforms.iOS
{
    public class NativeCameraView : UIView
    {
        public event EventHandler LayoutFinishedEvent;

        bool LayoutFinished = false;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // Keep the camera picture the same size as this view if the view is resized after the camera opened
            if (Layer.Sublayers != null)
            {
                CoreAnimation.CATransaction.Begin();
                CoreAnimation.CATransaction.DisableActions = true;
                foreach (CoreAnimation.CALayer sublayer in Layer.Sublayers)
                {
                    sublayer.Frame = Bounds;
                }
                CoreAnimation.CATransaction.Commit();
            }

            if (!LayoutFinished)
            {
                LayoutFinished = true;
                LayoutFinishedEvent.Invoke(this, EventArgs.Empty);
            }

        }
    }
}
