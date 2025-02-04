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

            if (!LayoutFinished)
            {
                LayoutFinished = true;
                LayoutFinishedEvent.Invoke(this, EventArgs.Empty);
            }

        }
    }
}
