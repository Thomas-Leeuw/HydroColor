using Android.Media;

namespace HydroColor.Platforms.Android.Callbacks
{
    public class OnImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        public Action<ImageReader> OnImageAvailableAction;

        public void OnImageAvailable(ImageReader reader) => OnImageAvailableAction(reader);

    }
}
