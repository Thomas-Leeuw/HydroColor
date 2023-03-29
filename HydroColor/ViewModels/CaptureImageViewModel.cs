using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
using HydroColor.Resources.Strings;
using HydroColor.Services;
using HydroColor.Views;
using System.Numerics;

namespace HydroColor.ViewModels
{

    public enum SensorControl
    {
        START,
        STOP
    }

    [QueryProperty(nameof(ImageType), "ImageType")]
    [QueryProperty(nameof(CurrentLocation), "CurrentLocation")]
    public partial class CaptureImageViewModel : ObservableObject
    {
        [ObservableProperty]
        bool showNumericTiltAndHeading = false; // display values for tilt, heading, and sun angles (for debugging only)

        [ObservableProperty]
        HydroColorImageType imageType;

        [ObservableProperty]
        HydroColorRawImageData imageData;

        [ObservableProperty]
        Location currentLocation;

        [ObservableProperty]
        double trueNorthHeading = double.NaN;

        [ObservableProperty]
        double deviceOffNadirAngle = double.NaN;

        [ObservableProperty]
        double inclinometerYPosition;

        [ObservableProperty]
        double compassDisplayRotation;

        [ObservableProperty]
        double sunElevationAngle;

        [ObservableProperty]
        double sunAzimuth;

        [ObservableProperty]
        double targetAzimuthArrowRotation;

        [ObservableProperty]
        bool waitingForImageResults;

        [ObservableProperty]
        bool compassVisible = true;

        [ObservableProperty]
        string imageCollectionHelpText = "";

        [ObservableProperty]
        Color captureButtonColor = Colors.White;

        double magneticDeclination;

        public Action CaptureImage;

        HydroColorImageCaptureModel CapturedImageData;

        // used to correct a 180 flip in the compass above certain device tilt
        double compassHeadingCorrection = 0;

        [RelayCommand]
        void CameraDidNotOpen()
        {
            Shell.Current.GoToAsync($"..", true);
        }

        [RelayCommand]
        void ViewAppearing()
        {
            if (CurrentLocation.Latitude == 0 || CurrentLocation.Longitude == 0)
            {
                ImageCollectionHelpText = Strings.CaptureImage_NoGPSNoHeadingHelp;
                CompassVisible = false;
                CurrentLocation.Accuracy = 0;
            }
            else
            {

                magneticDeclination = MagneticDeclination.GetDeclination(CurrentLocation.Latitude, CurrentLocation.Longitude);
                SunAngles sunAngles = SunModel.GetSunPosition(DateTime.UtcNow, CurrentLocation.Latitude, CurrentLocation.Longitude);
                SunElevationAngle = sunAngles.ElevationAngle;
                SunAzimuth = sunAngles.Azimuth;

                TargetAzimuthArrowRotation = -ImageType.TargetAzimuthAngleFromSun - SunAzimuth;

                ToggleCompass(SensorControl.START);
            }

            ToggleOrientation(SensorControl.START);
        }

        [RelayCommand]
        void ViewUnloaded()
        {
            ToggleCompass(SensorControl.STOP);
            ToggleOrientation(SensorControl.STOP);
        }

        [RelayCommand]
        void CompassHelp()
        {
            var CompassHelpPopup = new CompassCalibrationHelpPopup();
            CompassHelpPopup.BindingContext = this;

            Shell.Current.CurrentPage.ShowPopup(CompassHelpPopup);
        }

        [RelayCommand]
        void AdditionalCompassHelp()
        {
            try
            {
                Uri uri = new Uri("https://www.youtube.com/watch?v=-Uq7AmSAjt8");
                Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch
            {

            }

        }

        [RelayCommand]
        void CaptureButton()
        {
            WaitingForImageResults = true;
            CapturedImageData = new();

            CapturedImageData.ImageCapturedAtCorrectAngles = VerifyImageAngles();
            CapturedImageData.ImageLocation = CurrentLocation;
            CapturedImageData.MagneticDeclination = magneticDeclination;
            CapturedImageData.SunElevationAngle = SunElevationAngle;
            CapturedImageData.SunAzimuthAngle = SunAzimuth;
            CapturedImageData.ImageAzimuthAngle = TrueNorthHeading;
            CapturedImageData.ImageOffNadirAngle = DeviceOffNadirAngle;

            ToggleCompass(SensorControl.STOP);
            ToggleOrientation(SensorControl.STOP);
            try
            {
                CaptureImage();
                // Wait for OnImageDataChanged callback
            }
            catch (NotSupportedException ex)
            {
                Shell.Current.CurrentPage.DisplayAlert("Camera Not Supported", ex.Message, "OK");
                Shell.Current.GoToAsync($"..", true);
            }
            catch(Exception ex)
            {
                Shell.Current.CurrentPage.DisplayAlert("Camera Error", ex.Message, "OK");
                Shell.Current.GoToAsync($"..", true);
            }
            
        }

        async partial void OnImageDataChanged(HydroColorRawImageData value)
        {
            if (value != null)
            {
                await Task.Factory.StartNew(() => 
                { 
                    CapturedImageData.LocalTimeStamp = DateTime.Now;
                    CapturedImageData.UTCOffset = DateTimeOffset.Now.Offset.TotalHours;

                    // Downsample the raw image as soon as possible to reduce memory usage
                    BayerPatternDemosaic BPD = new BayerPatternDemosaic();

                    // Convert Byte array to Uint16 values (and reshape into 2d array).
                    UInt16[,] reshapedImageData = new UInt16[ImageData.ImageHeight, ImageData.RowStride / 2];
                    Buffer.BlockCopy(ImageData.RawImagePixelData, 0, reshapedImageData, 0, ImageData.ImageHeight * ImageData.RowStride);

                    // Demosaic Bayer filter pattern
                    ColorChannelData<UInt16[,]> RGBImages = BPD.Bayer2RGB(reshapedImageData, ImageData.ImageHeight, ImageData.ImageWidth, ImageData.BayerFilterPattern);

                    // Only use the center of the image. Take a square at the center 1/4 the size of the smallest dimension.
                    int minDimension = Math.Min(RGBImages.Red.GetLength(0), RGBImages.Red.GetLength(1));
                    CapturedImageData.MedianPixelValue.Red = BPD.GetMedian(RGBImages.Red, minDimension / 4);
                    CapturedImageData.MedianPixelValue.Green = BPD.GetMedian(RGBImages.Green, minDimension / 4);
                    CapturedImageData.MedianPixelValue.Blue = BPD.GetMedian(RGBImages.Blue, minDimension / 4);

                    CapturedImageData.BlackLevel = ImageData.BlackLevel;
                    CapturedImageData.SensorSensitivity = ImageData.SensorSensitivity;
                    CapturedImageData.ExposureTime = ImageData.ExposureTime;
                    CapturedImageData.JpegImage = ImageData.JpegImage;
                    CapturedImageData.BayerFilterPattern = ImageData.BayerFilterPattern;

                    // clearing out the very large ImageData and RGBImages instances (Should go out of scope and be garbage collected regardless).
                    RGBImages = null;
                    ImageData = null;

                    // Was getting intermittitent error on Android: 'Only the original thread that created a view hierarchy can touch its views'
                    // invoking this on the main thread seems to have solved this issue, but not 100% certain
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.GoToAsync($"..", true, new Dictionary<string, object>
                        {
                            [ImageType.HCImageTag.ToString()] = CapturedImageData
                        });

                    });

                });

            }

        }

        private void ToggleCompass(SensorControl sensorControl)
        {
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring && sensorControl == SensorControl.START)
                {
                    // Turn on compass
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI, applyLowPassFilter: true); // low pass filter only applies to Android devices
                }
                else if (Compass.Default.IsMonitoring && sensorControl == SensorControl.STOP)
                {
                    // Turn off compass
                    Compass.Default.Stop();
                    Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                }
            }
            else if (sensorControl == SensorControl.START) // Attempting to start sensor that is not supported
            {
                Shell.Current.CurrentPage.DisplayAlert(Strings.CaptureImage_NoCompassTitle, Strings.CaptureImage_NoCompassMessage, Strings.CaptureImage_NoCompassDismissButton);

            }
            
        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            TrueNorthHeading = (e.Reading.HeadingMagneticNorth + magneticDeclination + compassHeadingCorrection) % 360;
            CompassDisplayRotation = -e.Reading.HeadingMagneticNorth - magneticDeclination + compassHeadingCorrection;

            VerifyImageAngles();
        }

        private void ToggleOrientation(SensorControl sensorControl)
        {
            if (OrientationSensor.Default.IsSupported)
            {
                if (!OrientationSensor.Default.IsMonitoring && sensorControl == SensorControl.START)
                {
                    // Turn on compass
                    OrientationSensor.Default.ReadingChanged += Orientation_ReadingChanged;
                    OrientationSensor.Default.Start(SensorSpeed.UI);
                }
                else if (OrientationSensor.Default.IsMonitoring && sensorControl == SensorControl.STOP)
                {
                    // Turn off compass
                    OrientationSensor.Default.Stop();
                    OrientationSensor.Default.ReadingChanged -= Orientation_ReadingChanged;
                }
            }
            else if (sensorControl == SensorControl.START) // Attempting to start sensor that is not supported
            {
                Shell.Current.CurrentPage.DisplayAlert(Strings.CaptureImage_NoOrientationTitle, Strings.CaptureImage_NoOrientationMessage, Strings.CaptureImage_NoOrientationDismissButton);

            }
        }

        private void Orientation_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            // Update UI Label with orientation state
            Vector3 a = ToEulerAngles(e.Reading.Orientation);
            DeviceOffNadirAngle = a.X;
            UpdateInclinometerDisplay(a.X, ImageType.TargetOffNadirAngle);

            CheckForFlippedCompass(a.X);
            VerifyImageAngles();
        }

        private void UpdateInclinometerDisplay(double currentOffNadirAngle, double targetAngle)
        {
            double sensitivity = 4;
            InclinometerYPosition = (currentOffNadirAngle - targetAngle) * sensitivity;
        }

        private void CheckForFlippedCompass(float tiltAngle)
        {
            // Device tilt in degrees where the compass flips 180 degrees
            int CompassInvertAngle = 0;
#if ANDROID
            CompassInvertAngle = 90;         
#endif

#if IOS
            CompassInvertAngle = 135;
#endif

            if (tiltAngle > CompassInvertAngle)
            {
                compassHeadingCorrection = 180;
            }
            else
            {
                compassHeadingCorrection = 0;
            }
        }

        bool VerifyImageAngles()
        {
            double TargetAngle1 = (SunAzimuth + 135) % 360;
            double TargetAngle2 = SunAzimuth - 135;
            TargetAngle2 = TargetAngle2 > 0 ? TargetAngle2 : 360 + TargetAngle2;

            double TargetAngle1_OffsetDeg = ((TrueNorthHeading - TargetAngle1 + 540) % 360) - 180;
            double TargetAngle2_OffsetDeg = ((TrueNorthHeading - TargetAngle2 + 540) % 360) - 180;

            bool GoodHeading = (Math.Abs(TargetAngle1_OffsetDeg) < 8) || (Math.Abs(TargetAngle2_OffsetDeg) < 8);
            bool GoodTilt = Math.Abs(DeviceOffNadirAngle - ImageType.TargetOffNadirAngle) < 3;

            if (GoodTilt && GoodHeading)
            {  
                CaptureButtonColor = Colors.LimeGreen;
                return true;
            }
            else
            {
                CaptureButtonColor = Colors.White;
                return false;
            }
        }

        private Vector3 ToEulerAngles(Quaternion q)
        {
            Vector3 angles = new();

            // roll / x
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            angles.X *= (float)(180 / Math.PI);
            angles.Y *= (float)(180 / Math.PI);
            angles.Z *= (float)(180 / Math.PI);

            return angles;
        }
    }

    
}
