using Android.App;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;
using Size = Android.Util.Size;
using View = Android.Views.View;

namespace HydroColor.Platforms.Android
{
    public class CustomCameraView : FrameLayout, TextureView.ISurfaceTextureListener
    {
        public event EventHandler LayoutFinishedEvent;

        View view;
        public AutoFitTextureView textureView;

        public CustomCameraView() : base(Platform.CurrentActivity)
        {
            SetupView();
        }

        void SetupView()
        {

            Activity activity = this.Context as Activity;
            view = activity.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);

            textureView = view.FindViewById<AutoFitTextureView>(Resource.Id.textureView);
            textureView.SurfaceTextureListener = this;

            AddView(view);

        }

        public void SetViewDimensions(CameraCharacteristics cameraCharacteristics)
        {
            ConfigureTransform(cameraCharacteristics, textureView.Width, textureView.Height);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            view.Measure(msw, msh);

            view.Layout(0, Height / 2 - textureView.Height / 2, r - l, b - t);

        }

        #region ISurfaceTextureListener

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            LayoutFinishedEvent.Invoke(this, EventArgs.Empty);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }

        #endregion


        #region ConfigureCameraPreviewTransform

        Size ConfigureTransform(CameraCharacteristics cameraCharacteristics, int viewWidth, int viewHeight)
        {
            if (cameraCharacteristics == null)
            {
                return new Size(0, 0);
            }

            var map = (StreamConfigurationMap)cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

            // For still image captures, we always use the largest available size.
            Size largestJpeg = (Size)Collections.Max(Arrays.AsList(map.GetOutputSizes((int)ImageFormatType.Jpeg)),
                                   new CompareSizesByArea());

            // Swap the view dimensions as needed if they are rotated relative to
            // the sensor.
            int sensorOrientation = (int)cameraCharacteristics.Get(CameraCharacteristics.SensorOrientation);
            bool swappedDimensions = sensorOrientation == 90 || sensorOrientation == 270;
            int rotatedViewWidth = viewWidth;
            int rotatedViewHeight = viewHeight;
            if (swappedDimensions)
            {
                rotatedViewWidth = viewHeight;
                rotatedViewHeight = viewWidth;
            }


            // Find the best preview size for these view dimensions and configured JPEG size.
            Size previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))),
                                   rotatedViewWidth, rotatedViewHeight, largestJpeg);

            if (swappedDimensions)
            {
                textureView.SetAspectRatio(
                    previewSize.Height, previewSize.Width);
            }
            else
            {
                textureView.SetAspectRatio(
                    previewSize.Width, previewSize.Height);
            }

            return previewSize;

        }

        Size ChooseOptimalSize(Size[] choices, int width, int height, Size aspectRatio)
        {
            // Collect the supported resolutions that are at least as big as the preview Surface
            List<Size> bigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;
            foreach (Size option in choices)
            {
                if (option.Height == option.Width * h / w &&
                    option.Width >= width && option.Height >= height)
                {
                    bigEnough.Add(option);
                }
            }

            // Pick the smallest of those, assuming we found any
            if (bigEnough.Count > 0)
            {
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            }
            else
            {
                // None found
                return choices[0];
            }
        }

        #endregion

    }

    class CompareSizesByArea : Java.Lang.Object, IComparator
    {
        public int Compare(Size lhs, Size rhs)
        {
            // We cast here to ensure the multiplications won't overflow
            return Long.Signum((long)lhs.Width * lhs.Height -
            (long)rhs.Width * rhs.Height);
        }

        int IComparator.Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
        {
            return 0;
        }

        bool IComparator.Equals(Java.Lang.Object @object)
        {
            return false;
        }
    }


}