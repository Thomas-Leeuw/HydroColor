using UIKit;

namespace HydroColor.Platforms.iOS
{
    public class CustomCameraView : UIView
    {
        public event EventHandler LayoutFinishedEvent;

        bool LayoutFinished = false;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (!LayoutFinished)
            {
                LayoutFinished = true;
                LayoutFinishedEvent.Invoke(this, EventArgs.Empty);
            }

        }
    }
}
