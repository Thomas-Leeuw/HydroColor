#if ANDROID
using CameraController = HydroColor.Platforms.Android.CameraController;
#elif IOS
using CameraController = HydroColor.Platforms.iOS.CameraController;
#endif

namespace HydroColor.ViewModels
{
    public class CameraPreview : View
    {
        public static readonly BindableProperty LayoutFinishedProperty = BindableProperty.Create(
        propertyName: nameof(LayoutFinished),
        returnType: typeof(bool),
        declaringType: typeof(CameraPreview),
        defaultValue: false,
        defaultBindingMode: BindingMode.OneWayToSource);
        public bool LayoutFinished
        {
            get { return (bool)GetValue(LayoutFinishedProperty); }
            set { SetValue(LayoutFinishedProperty, value); }
        }

        public static readonly BindableProperty CameraControlProperty = BindableProperty.Create(
        propertyName: nameof(CameraControl),
        returnType: typeof(CameraController),
        declaringType: typeof(CameraPreview),
        defaultValue: null,
        defaultBindingMode: BindingMode.TwoWay);

        public CameraController CameraControl
        {
            get { return (CameraController)GetValue(CameraControlProperty); }
            set { SetValue(CameraControlProperty, value); }
        }
    }
}
