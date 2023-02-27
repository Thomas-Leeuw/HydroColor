using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Views;
using Android.Widget;
using HydroColor.Models;
using HydroColor.ViewModels;
using Java.Lang;
using Java.Util;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using System.ComponentModel;
using Size = Android.Util.Size;
using View = Android.Views.View;

namespace HydroColor.Platforms.Android
{
    public class CameraPreviewRenderer : FrameLayout, IVisualElementRenderer, IViewRenderer, TextureView.ISurfaceTextureListener
    {
        public event EventHandler<VisualElementChangedEventArgs> ElementChanged;
        public event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;

        View view;
        AutoFitTextureView textureView;
        VisualElementTracker visualElementTracker;
        int? defaultLabelFor;
        CameraController mCameraController;

        CameraPreview cameraPreview;
        CameraPreview mCameraPreview
        {
            get => cameraPreview;
            set
            {
                if (cameraPreview == value)
                {
                    return;
                }

                var oldElement = cameraPreview;
                cameraPreview = value;
                OnElementChanged(new ElementChangedEventArgs<CameraPreview>(oldElement, cameraPreview));
            }
        }

        public CameraPreviewRenderer(Context context) : base(context)
        {
        }

        void OnElementChanged(ElementChangedEventArgs<CameraPreview> e)
        {
            
            if (e.OldElement != null)
            {
                e.OldElement.PropertyChanged -= OnElementPropertyChanged;
                e.OldElement.PropertyChanged -= OnImageCaptureRequested;
                mCameraController.ImageCaptureCompletedEvent -= OnImageCaptureCompleted;
                mCameraController = null;
            }
            if (e.NewElement != null)
            {
                this.EnsureId();

                e.NewElement.PropertyChanged += OnElementPropertyChanged;
                e.NewElement.ImageCaptureRequested += OnImageCaptureRequested;
                
                ElevationHelper.SetElevation(this, e.NewElement);
                mCameraController = new CameraController();
                mCameraController.ImageCaptureCompletedEvent += OnImageCaptureCompleted;
            }

            ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));

            try
            {
                SetupUserInterface();
                AddView(view);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"			ERROR: ", ex.Message);
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

        void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ElementPropertyChanged?.Invoke(this, e);
        }

        void SetupUserInterface()
        {

            Activity activity = this.Context as Activity;
            view = activity.LayoutInflater.Inflate(Resource.Layout.CameraLayout, this, false);

            textureView = view.FindViewById<AutoFitTextureView>(Resource.Id.textureView);
            textureView.SurfaceTextureListener = this;

            // This is the output Surface the preview will be displayed in
            mCameraController.mTextureView = textureView;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            view.Measure(msw, msh);

            view.Layout(0, Height/2 - textureView.Height/2, r - l, b - t);
        }

        #region ISurfaceTextureListener

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {

        }

        public async void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            textureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);

            if (!await mCameraController.OpenCamera())
            {
                mCameraPreview.CameraFailedToOpen();
                return;
            }

            Size previewSize = ConfigureTransform(mCameraController.mCharacteristics, width, height);
            SurfaceTexture texture = textureView.SurfaceTexture;
            texture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            mCameraController.CloseCamera();
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }

        #endregion

        #region IViewRenderer

        void IViewRenderer.MeasureExactly() => MeasureExactly(this, mCameraPreview, Context);

        static void MeasureExactly(View control, VisualElement element, Context context)
        {
            if (control == null || element == null)
            {
                return;
            }

            double width = element.Width;
            double height = element.Height;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            int realWidth = (int)context.ToPixels(width);
            int realHeight = (int)context.ToPixels(height);

            int widthMeasureSpec = MeasureSpecFactory.MakeMeasureSpec(realWidth, MeasureSpecMode.Exactly);
            int heightMeasureSpec = MeasureSpecFactory.MakeMeasureSpec(realHeight, MeasureSpecMode.Exactly);

            control.Measure(widthMeasureSpec, heightMeasureSpec);
        }

        static class MeasureSpecFactory
        {
            public static int GetSize(int measureSpec)
            {
                const int modeMask = 0x3 << 30;
                return measureSpec & ~modeMask;
            }

            public static int MakeMeasureSpec(int size, MeasureSpecMode mode) => size + (int)mode;
        }

        #endregion

        #region IVisualElementRenderer

        VisualElement IVisualElementRenderer.Element => mCameraPreview;

        VisualElementTracker IVisualElementRenderer.Tracker => visualElementTracker;

        View IVisualElementRenderer.View => this;

        SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
        {
            Measure(widthConstraint, heightConstraint);
            SizeRequest result = new SizeRequest(new Microsoft.Maui.Graphics.Size(MeasuredWidth, MeasuredHeight), new Microsoft.Maui.Graphics.Size(Context.ToPixels(20), Context.ToPixels(20)));
            return result;
        }

        void IVisualElementRenderer.SetElement(VisualElement element)
        {
            if (!(element is CameraPreview camera))
            {
                throw new ArgumentException($"{nameof(element)} must be of type {nameof(CameraPreview)}");
            }

            if (visualElementTracker == null)
            {
                visualElementTracker = new VisualElementTracker(this);
            }
            mCameraPreview = camera;
        }

        void IVisualElementRenderer.SetLabelFor(int? id)
        {
            if (defaultLabelFor == null)
            {
                defaultLabelFor = LabelFor;
            }
            LabelFor = (int)(id ?? defaultLabelFor);
        }

        void IVisualElementRenderer.UpdateLayout() => visualElementTracker?.UpdateLayout();

        #endregion

        #region ConfigureCameraPreviewTransform

        Size ConfigureTransform(CameraCharacteristics cameraCharacteristics, int viewWidth, int viewHeight)
        {


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
                //Log.Error(TAG, "Couldn't find any suitable preview size");
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
