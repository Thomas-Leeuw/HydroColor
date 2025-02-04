using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using View = Android.Views.View;

namespace HydroColor.Platforms.Android
{
    public class NativeCameraView : FrameLayout, TextureView.ISurfaceTextureListener
    {
        public event EventHandler LayoutFinishedEvent;
        public AutoFitTextureView textureView { get; private set; }

        View view;

        public NativeCameraView() : base(Platform.CurrentActivity)
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

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            view.Measure(msw, msh);

            view.Layout(0, Height / 2 - textureView.Height / 2, r - l, b - t);

        }

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
    }

}