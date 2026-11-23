using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
using HydroColor.Resources.Strings;
using HydroColor.Services;
using HydroColor.Views;
using System.Numerics;
using static Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using CameraController = HydroColor.Platforms.Android.CameraController;
#elif IOS
using CameraController = HydroColor.Platforms.iOS.CameraController;
#endif

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
        bool showNumericTiltAndHeading = false; // display values for tilt, heading, and sun angles (shown in camera diagnostics mode)

        [ObservableProperty]
        HydroColorImageType imageType;

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

        [ObservableProperty]
        bool cameraPreviewLayoutFinished;

        [ObservableProperty]
        CameraController camControl = new CameraController();

        double magneticDeclination;

        HydroColorImageCaptureModel CapturedImageData;

        // used to correct a 180 flip in the compass above certain device tilt
        double compassHeadingCorrection = 0;

        // true while this page is on screen (between ViewAppearing and ViewUnloaded)
        bool _pageActive;

        // true once the result of a capture (success or failure) has been acted on.
        // The camera can report more than once, for example for the raw and the processed photo on iOS.
        bool _captureResultHandled;

        // true while OpenCameraPreview is running, so two triggers cannot open the camera twice
        bool _openingCamera;

        public CaptureImageViewModel()
        {
            camControl.ImageCaptureCompletedEvent += ImageCaptureCompleted;
        }

        // need to wait for the layout to finish before opening the camera
        async partial void OnCameraPreviewLayoutFinishedChanged(bool value)
        {
            if (!value)
            {
                // The preview surface was destroyed: the app went to the background or the page is closing.
                CamControl.CloseCamera();
                return;
            }

            await OpenCameraPreview();
        }

        // Called when the app returns to the foreground.
        // The camera can be taken away while the app is not in front (screen locked, another camera app opened)
        // without the preview surface being recreated, so it is reopened here if needed.
        async void Window_Resumed(object sender, EventArgs e)
        {
            if (_pageActive && CameraPreviewLayoutFinished)
            {
                await OpenCameraPreview();
            }
        }

        // Opens the camera and starts the preview, unless it is already open
        async Task OpenCameraPreview()
        {
            if (_captureResultHandled)
            {
                return; // a capture has already finished and the page is closing
            }

            if (WaitingForImageResults)
            {
                // The app went to the background during a capture and the camera was closed before the image arrived.
                _captureResultHandled = true;
                CaptureFailed("The app was moved to the background during the capture.");
                return;
            }

            if (CamControl.IsOpen || _openingCamera)
            {
                return;
            }
            _openingCamera = true;

            try
            {
                // Check camera permission
                PermissionStatus permissionStatus = await HardwarePermissions.RequestPermission<Camera>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    await Shell.Current.GoToAsync($"..", true);
                    return;
                }

                try
                {
                    CamControl.OpenCamera();
                }
                catch (NotSupportedException)
                {
                    await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CameraController_NoCameraTitle, Strings.CameraController_NoCameraMessage, Strings.CameraController_NoCameraDismissButton);
                    await Shell.Current.GoToAsync($"..", true);
                }
                catch (InvalidOperationException)
                {
                    await Shell.Current.CurrentPage.DisplayAlertAsync("Camera Session", "A camera session could not be opened.", "OK");
                    await Shell.Current.GoToAsync($"..", true);
                }
                catch (Exception ex)
                {
                    await Shell.Current.CurrentPage.DisplayAlertAsync("Camera Error", ex.Message, "OK");
                    await Shell.Current.GoToAsync($"..", true);
                }
            }
            finally
            {
                _openingCamera = false;
            }

        }

        [RelayCommand]
        void ViewAppearing()
        {
            _pageActive = true;

            // In camera diagnostics mode, show the tilt, heading and sun angles as numbers
            ShowNumericTiltAndHeading = CameraDiagnostics.Enabled;

            // Reopen the camera if needed when the app returns to the foreground
            Shell.Current.Window.Resumed -= Window_Resumed;
            Shell.Current.Window.Resumed += Window_Resumed;

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
            _pageActive = false;
            Shell.Current.Window.Resumed -= Window_Resumed;

            ToggleCompass(SensorControl.STOP);
            ToggleOrientation(SensorControl.STOP);
            CamControl.CloseCamera();
        }

        [RelayCommand]
        void CompassHelp()
        {
            var CompassHelpPopup = new CompassCalibrationHelpPopup();
            CompassHelpPopup.BindingContext = this;

            Shell.Current.CurrentPage.ShowPopupAsync(CompassHelpPopup);
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
        async Task CaptureButton()
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
                CamControl.TakeRAWImage();
                // Wait for ImageCaptureCompleted callback
            }
            catch (NotSupportedException ex)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync("Camera Not Supported", ex.Message, "OK");
                await Shell.Current.GoToAsync($"..", true);
            }
            catch(Exception ex)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync("Camera Error", ex.Message, "OK");
                await Shell.Current.GoToAsync($"..", true);
            }
            
        }


        async void ImageCaptureCompleted(object sender, ImageCaptureEventArgs e)
        {
            if (_captureResultHandled)
            {
                return;
            }
            _captureResultHandled = true;

            try
            {
                // No image data means the camera reported that the capture failed
                if (e.ImageData == null)
                {
                    CaptureFailed(e.ErrorMessage);
                    return;
                }

                await Task.Factory.StartNew(() =>
                {
                    CapturedImageData.LocalTimeStamp = DateTime.Now;
                    CapturedImageData.UTCOffset = DateTimeOffset.Now.Offset.TotalHours;

                    // Reduce the raw image to the median value of each color as soon as possible, to reduce memory usage
                    CapturedImageData.MedianPixelValue = BayerPatternDemosaic.GetMedianPixelValues(e.ImageData);

                    CapturedImageData.BlackLevel = e.ImageData.BlackLevel;
                    CapturedImageData.SensorSensitivity = e.ImageData.SensorSensitivity;
                    CapturedImageData.ExposureTime = e.ImageData.ExposureTime;
                    CapturedImageData.JpegImage = e.ImageData.JpegImage;
                    CapturedImageData.BayerFilterPattern = e.ImageData.BayerFilterPattern;
                    CapturedImageData.CameraId = e.ImageData.CameraId;
                    CapturedImageData.RawImageWidth = e.ImageData.ImageWidth;
                    CapturedImageData.RawImageHeight = e.ImageData.ImageHeight;

                    // Camera diagnostics (null unless diagnostics mode is on)
                    CapturedImageData.Diagnostics = e.ImageData.Diagnostics;
                    if (CapturedImageData.Diagnostics != null)
                    {
                        AddProcessingDiagnostics(CapturedImageData);
                    }

                    // clearing out the very large ImageData instance (Should go out of scope and be garbage collected regardless).
                    e.ImageData = null;

                });

                // Was getting intermittitent error on Android: 'Only the original thread that created a view hierarchy can touch its views'
                // invoking this on the main thread seems to have solved this issue, but not 100% certain
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (!_pageActive)
                    {
                        return; // the user left the page while the image was being processed
                    }

                    try
                    {
                        await Shell.Current.GoToAsync($"..", true, new Dictionary<string, object>
                        {
                            [ImageType.HCImageTag.ToString()] = CapturedImageData
                        });
                    }
                    catch
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                // Any error while processing the image is reported to the user instead of closing the app
                CaptureFailed(ex.Message);
            }

        }

        // Adds the values actually used for processing to the top of the diagnostics list.
        // They are taken from the stored image data, not read from the camera again.
        static void AddProcessingDiagnostics(HydroColorImageCaptureModel image)
        {
            try
            {
                CaptureDiagnostics used = new();
                ColorChannelData<double> m = image.MedianPixelValue;
                ColorChannelData<double> b = image.BlackLevel;
                double? whiteLevel = image.Diagnostics.WhiteLevel;

                used.Add("USED exposure time (s)", () => image.ExposureTime);
                used.Add("USED sensitivity", () => image.SensorSensitivity);
                used.Add("USED black level (R G B)", () => new[] { b.Red, b.Green, b.Blue });
                used.Add("USED median count (R G B)", () => new[] { m.Red, m.Green, m.Blue });

                // The light level of this image, from the same function the measurement uses
                used.Add("Computed light level (R G B)", () =>
                {
                    ColorChannelData<double> level = OpticalPropertiesCalculator.ComputeRelativeLightLevel(image);
                    return new[] { level.Red, level.Green, level.Blue };
                });
                used.Add("Counts above black (R G B)", () => new[] { m.Red - b.Red, m.Green - b.Green, m.Blue - b.Blue });
                used.Add("Percent of range (R G B)", () =>
                {
                    if (whiteLevel == null)
                    {
                        return null;
                    }
                    double w = whiteLevel.Value;
                    return new[]
                    {
                        Math.Round(100 * (m.Red - b.Red) / (w - b.Red), 1),
                        Math.Round(100 * (m.Green - b.Green) / (w - b.Green), 1),
                        Math.Round(100 * (m.Blue - b.Blue) / (w - b.Blue), 1)
                    };
                });

                // Lines the camera code marked USED (on iOS, the camera ISO and baseline exposure that give the
                // sensitivity) are moved up to sit directly under the sensitivity line.
                List<KeyValuePair<string, string>> cameraUsed = image.Diagnostics.Items.Where(i => i.Key.StartsWith("USED")).ToList();
                image.Diagnostics.Items.RemoveAll(i => i.Key.StartsWith("USED"));
                used.Items.InsertRange(2, cameraUsed);

                image.Diagnostics.Items.InsertRange(0, used.Items);
            }
            catch
            {
            }
        }

        // Shows the capture failed message and returns to the previous page
        void CaptureFailed(string detail)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                WaitingForImageResults = false;

                if (!_pageActive)
                {
                    return; // the user already left the page
                }

                try
                {
                    string message = Strings.CaptureImage_CaptureFailedMessage;
                    if (!string.IsNullOrEmpty(detail))
                    {
                        message += "\n\n" + detail;
                    }

                    await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CaptureImage_CaptureFailedTitle, message, Strings.CaptureImage_CaptureFailedDismissButton);
                    await Shell.Current.GoToAsync($"..", true);
                }
                catch
                {
                }
            });
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
                Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CaptureImage_NoCompassTitle, Strings.CaptureImage_NoCompassMessage, Strings.CaptureImage_NoCompassDismissButton);

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
                Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CaptureImage_NoOrientationTitle, Strings.CaptureImage_NoOrientationMessage, Strings.CaptureImage_NoOrientationDismissButton);

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
