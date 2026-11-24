using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
using HydroColor.Resources.Strings;
using HydroColor.Services;
using HydroColor.Views;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;

namespace HydroColor.ViewModels
{
    [QueryProperty(nameof(GrayCardImageData), nameof(HydroColorImageTag.GrayCard))]
    [QueryProperty(nameof(WaterImageData), nameof(HydroColorImageTag.Water))]
    [QueryProperty(nameof(SkyImageData), nameof(HydroColorImageTag.Sky))]
    public partial class CollectDataViewModel : ObservableObject
    {
        public Action<MapSpan> MoveMapLocationAction;

        public ObservableCollection<MapPinModel> MapPins { get; set; } = new();

        [ObservableProperty]
        Location currentLocation;

        [ObservableProperty]
        ImageSource grayCardThumbnailImage;

        [ObservableProperty]
        ImageSource waterThumbnailImage;

        [ObservableProperty]
        ImageSource skyThumbnailImage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnalyzeButtonOpacity))]
        HydroColorImageCaptureModel grayCardImageData;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnalyzeButtonOpacity))]
        HydroColorImageCaptureModel waterImageData;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AnalyzeButtonOpacity))]
        HydroColorImageCaptureModel skyImageData;

        // The Analyze Images button is drawn faint until all three images have been taken.
        // It is not disabled: disabling it caused a crash on an older Android phone (see AnalyzeImages).
        public double AnalyzeButtonOpacity => GrayCardImageData != null && SkyImageData != null && WaterImageData != null ? 1.0 : 0.4;

        [ObservableProperty]
        HydroColorImageType grayCardImageType = new()
        {
            HCImageTag = HydroColorImageTag.GrayCard,
            TargetOffNadirAngle = 40,
            TargetAzimuthAngleFromSun = 135,
            DisplayName = Strings.CollectData_GrayCardDisplayName
        };
        [ObservableProperty]
        HydroColorImageType waterImageType = new()
        {
            HCImageTag = HydroColorImageTag.Water,
            TargetOffNadirAngle = 40,
            TargetAzimuthAngleFromSun = 135,
            DisplayName = Strings.CollectData_WaterDisplayName
        };
        [ObservableProperty]
        HydroColorImageType skyImageType = new()
        {
            HCImageTag = HydroColorImageTag.Sky,
            TargetOffNadirAngle = 130,
            TargetAzimuthAngleFromSun = 135,
            DisplayName = Strings.CollectData_SkyDisplayName
        };

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UserInputLongitudeCommand))]
        [NotifyCanExecuteChangedFor(nameof(UserInputLatitudeCommand))]
        [NotifyCanExecuteChangedFor(nameof(GetCurrentLocationCommand))]
        bool canCheckLocation;

        [ObservableProperty]
        bool haveGrayCardData;

        [ObservableProperty]
        Color grayCardImageSquareColor = Colors.White;

        [ObservableProperty]
        bool haveSkyData;

        [ObservableProperty]
        Color skyImageSquareColor = Colors.White;

        [ObservableProperty]
        bool haveWaterData;

        [ObservableProperty]
        Color waterImageSquareColor = Colors.White;

        [ObservableProperty]
        bool sunElevationAngleWarningVisible;

        // true while camera diagnostics mode is on (shows a reminder label)
        [ObservableProperty]
        bool diagnosticsOn;

        private CancellationTokenSource _cancelTokenSource;
        private bool _viewLoaded = false;

        // true when the current coordinates were typed in by the user (not from GPS)
        private bool _locationEnteredManually = false;

        public CollectDataViewModel()
        {

            ResetImageCapture();
            Shell.Current.Window.Resumed += Window_Resumed;

            VersionCorrections.UpdateTo2p1DataFileFormatIfNeeded();
        }

        private async void Window_Resumed(object sender, EventArgs e)
        {
            // Coordinates typed in by the user are kept. They were entered because GPS was not giving a position.
            if (_locationEnteredManually)
            {
                return;
            }

            // A location request is already running (for example, the app resumed after the permission prompt)
            if (!CanCheckLocation)
            {
                return;
            }

            // Refresh the GPS position. The previous position stays on screen until the new one arrives.
            await GetCurrentLocation();
        }

        [RelayCommand]
        async Task ViewLoaded()
        {
            // Loaded event is firing every time we navigate to the tab, event though it is registered as singleton.
            // Not sure why.
            // We only need to run this once at startup.
            if (!_viewLoaded) 
            {
                _viewLoaded = true;
                CurrentLocation = new Location();
                CurrentLocation.Accuracy = 0;
                OnPropertyChanged(nameof(CurrentLocation));
                MoveMapLocationAction(new MapSpan(new Location { Longitude = 0, Latitude = 0 }, 180, 180));
                await GetCurrentLocation();
            }
            
        }

        [RelayCommand]
        void ViewAppearing()
        {
            CheckSunElevationAngle();
            DiagnosticsOn = CameraDiagnostics.Enabled;
        }

        // Shows the camera values of a new image. Does nothing unless diagnostics mode was on for the capture.
        async void ShowImageDiagnostics(string imageName, HydroColorImageCaptureModel image)
        {
            if (image?.Diagnostics == null)
            {
                return;
            }

            try
            {
                string text = image.Diagnostics.ToDisplayText();
                System.Diagnostics.Debug.WriteLine($"HydroColor diagnostics: {imageName}\n{text}");

                await Task.Delay(600); // let the return from the capture page finish first
                if (Shell.Current.CurrentPage is not CollectDataView)
                {
                    return;
                }

                bool copy = await Shell.Current.CurrentPage.DisplayAlertAsync($"Camera Diagnostics: {imageName}", text, "Copy", "Close");
                if (copy)
                {
                    await Clipboard.Default.SetTextAsync($"{imageName}\n{text}");
                }
            }
            catch
            {
            }
        }

        void ResetImageCapture()
        {
            SkyImageData = null;
            WaterImageData = null;
            GrayCardImageData = null;

            HaveGrayCardData = false;
            GrayCardImageSquareColor = Colors.White;
            HaveSkyData = false;
            SkyImageSquareColor = Colors.White;
            HaveWaterData = false;
            WaterImageSquareColor = Colors.White;

            GrayCardThumbnailImage = ImageSource.FromFile("graycard_icon.png");
            SkyThumbnailImage = ImageSource.FromFile("sky_icon.png");
            WaterThumbnailImage = ImageSource.FromFile("water_icon.png");
        }

        partial void OnGrayCardImageDataChanged(HydroColorImageCaptureModel value)
        {
            if (value != null)
            {
                GrayCardThumbnailImage = ImageSource.FromStream(() => new MemoryStream(GrayCardImageData.JpegImage));
                HaveGrayCardData = true;
                GrayCardImageSquareColor = GrayCardImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
                ShowImageDiagnostics(Strings.CollectData_GrayCardDisplayName, value);
            }
            else
            {
                GrayCardThumbnailImage = ImageSource.FromFile("graycard_icon.png");
            }
        }
        partial void OnWaterImageDataChanged(HydroColorImageCaptureModel value)
        {
            if (value != null)
            {
                WaterThumbnailImage = ImageSource.FromStream(() => new MemoryStream(WaterImageData.JpegImage));
                HaveWaterData = true;
                WaterImageSquareColor = WaterImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
                ShowImageDiagnostics(Strings.CollectData_WaterDisplayName, value);
            }
            else
            {
                WaterThumbnailImage = ImageSource.FromFile("water_icon.png");
            }
        }
        partial void OnSkyImageDataChanged(HydroColorImageCaptureModel value)
        {
            if (value != null)
            {
                SkyThumbnailImage = ImageSource.FromStream(() => new MemoryStream(SkyImageData.JpegImage));
                HaveSkyData = true;
                SkyImageSquareColor = SkyImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
                ShowImageDiagnostics(Strings.CollectData_SkyDisplayName, value);
            }
            else
            {
                SkyThumbnailImage = ImageSource.FromFile("sky_icon.png");
            }
        }

        [RelayCommand]
        async Task CaptureImageTapped(HydroColorImageType imageType)
        {

            await Shell.Current.GoToAsync(nameof(CaptureImageView), new Dictionary<string, object>
            {
                ["CurrentLocation"] = CurrentLocation,
                ["ImageType"] = imageType
            });

        }

        [RelayCommand(CanExecute = nameof(CanCheckLocation))]
        async Task GetCurrentLocation()
        {
            
            try
            {
                CanCheckLocation = false;
                // GeolocationRequest will prompt for location permission
                GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(20));
                request.RequestFullAccuracy = true; // only affects iOS
                _cancelTokenSource = new CancellationTokenSource();
                Location NewLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
                if (NewLocation != null)
                {
                    CurrentLocation = NewLocation;
                    _locationEnteredManually = false;
                    MoveMapPinLocation();
                }
                else
                {
                    // No new position. Keep showing the previous one if there is one;
                    // otherwise show the whole map, as at startup.
                    MoveMapPinLocation();
                    if (CurrentLocation.Latitude == 0 || CurrentLocation.Longitude == 0)
                    {
                        MoveMapLocationAction(new MapSpan(new Location { Longitude = 0, Latitude = 0 }, 180, 180));
                    }
                    await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_NoGPSTitle, Strings.CollectData_NoGPSMessage, Strings.CollectData_NoGPSDismissButton);
                }
            }
            catch (PermissionException)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_LocationPermissionDeniedTitle, Strings.CollectData_LocationPermissionDeniedMessage, Strings.CollectData_LocationPermissionDeniedDismissButton);
            }
            catch (FeatureNotSupportedException)
            {
                // The device has no GPS
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_GPSNotSupportedTitle, Strings.CollectData_GPSNotSupportedMessage, Strings.CollectData_GPSNotSupportedDismissButton);
            }
            catch
            {
                // Anything else (location services switched off, a timeout, a cancelled request):
                // the position could not be determined this time, so offer a retry or manual entry.
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_NoGPSTitle, Strings.CollectData_NoGPSMessage, Strings.CollectData_NoGPSDismissButton);
            }
            finally
            {
                CanCheckLocation = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanCheckLocation))]
        async Task UserInputLatitude()
        {

            string lat = await Shell.Current.CurrentPage.DisplayPromptAsync(Strings.CollectData_EnterLatitudeTitle, Strings.CollectData_EnterLatitudeMessage, initialValue: CurrentLocation.Latitude.ToString("F5"));
            if (lat == null) // cancel pressed
            {
                return;
            }
            if (double.TryParse(lat, out double Latitude))
            {
                if (Math.Abs(Latitude) <= 90)
                {
                    _locationEnteredManually = true;
                    CurrentLocation.Accuracy = 0;
                    CurrentLocation.Latitude = Latitude;
                    OnPropertyChanged(nameof(CurrentLocation));
                    MoveMapPinLocation();
                    CheckSunElevationAngle();
                }
            }

        }

        [RelayCommand(CanExecute = nameof(CanCheckLocation))]
        async Task UserInputLongitude()
        {
            string lon = await Shell.Current.CurrentPage.DisplayPromptAsync(Strings.CollectData_EnterLongitudeTitle, Strings.CollectData_EnterLongitudeMessage, initialValue: CurrentLocation.Longitude.ToString("F5"));
            if (lon == null) // cancel pressed
            {
                return;
            }
            if (double.TryParse(lon, out double Longitude))
            {
                if (Math.Abs(Longitude) <= 180)
                {

                    _locationEnteredManually = true;
                    CurrentLocation.Accuracy = 0;
                    CurrentLocation.Longitude = Longitude;
                    OnPropertyChanged(nameof(CurrentLocation));
                    MoveMapPinLocation();
                    CheckSunElevationAngle();
                }
            }

        }

        void MoveMapPinLocation()
        {
            MapPins.Clear();
            if (CurrentLocation.Latitude != 0 && CurrentLocation.Longitude != 0)
            {
                MapPins.Add(new MapPinModel { location = CurrentLocation });
                MoveMapLocationAction(new MapSpan(CurrentLocation, 0.02, 0.02));
            }
        }

        partial void OnCurrentLocationChanged(Location value)
        {
            MoveMapPinLocation();
            CheckSunElevationAngle();
        }

        double CheckSunElevationAngle()
        {
            if (CurrentLocation != null && (CurrentLocation.Latitude != 0 && CurrentLocation.Longitude != 0))
            {
                SunAngles sunAngles = SunModel.GetSunPosition(DateTime.UtcNow, CurrentLocation.Latitude, CurrentLocation.Longitude);
                SunElevationAngleWarningVisible = sunAngles.ElevationAngle < 20 || sunAngles.ElevationAngle > 85;
                return sunAngles.ElevationAngle;
            }
            else
            {
                SunElevationAngleWarningVisible = false;
                return double.NaN;
            }
        }

        [RelayCommand]
        void SunElevationAngleWarningTapped()
        {
            double SunElevationAngle = CheckSunElevationAngle();
            Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_SunElevationWarningTitle, $"{Strings.CollectData_SunElevationWarningMessage_1} {SunElevationAngle.ToString("F0")} {Strings.CollectData_SunElevationWarningMessage_2}", Strings.CollectData_SunElevationWarningDismissButton);
        }

        [RelayCommand] // using CanExecute here causes crash on LGE Nexus 5, Android 6.0
        async Task AnalyzeImages()
        {
            if (GrayCardImageData != null && WaterImageData != null && SkyImageData != null)
            {

                // The three images are only comparable if they came from the same camera (lens and sensor)
                if (!ImagesFromSameCamera(GrayCardImageData, SkyImageData, WaterImageData))
                {
                    await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_DifferentCamerasTitle, Strings.CollectData_DifferentCamerasMessage, Strings.CollectData_DifferentCamerasDismissButton);
                    return;
                }

                string measurementName = await Shell.Current.CurrentPage.DisplayPromptAsync(Strings.CollectData_MeasurementNameTitle, Strings.CollectData_MeasurementNameMessage);
                if (measurementName == null) // cancel pressed
                {
                    return;
                }

                HydroColorProcessedMeasurement ProcMeas = new();
                ProcMeas.DeviceManufacturer = DeviceInfo.Manufacturer.Replace(" ","_");
                ProcMeas.DeviceModel = DeviceInfo.Model.Replace(" ", "_");
                ProcMeas.DeviceOSVersion = DeviceInfo.VersionString.Replace(" ", "_");

                ProcMeas.GrayCardImageData = GrayCardImageData;
                ProcMeas.SkyImageData = SkyImageData;
                ProcMeas.WaterImageData = WaterImageData;
                ProcMeas.MeasurementName = measurementName;
                ProcMeas.HydroColorVersion = AppInfo.Current.VersionString;

                ProcMeas.MeasurementProducts = OpticalPropertiesCalculator.ComputeOpticalProperties(GrayCardImageData, SkyImageData, WaterImageData);

                FileReaderWriter filewriter = new();
                filewriter.WriteDataRecord(ProcMeas);

                await Shell.Current.GoToAsync(nameof(DataView), true, new Dictionary<string, object>
                {
                    ["MeasurementData"] = ProcMeas
                });

                ResetImageCapture();
            }
            else
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.CollectData_IncompleteImagesTitle, Strings.CollectData_IncompleteImagesMessage, Strings.CollectData_IncompleteImagesDismissButton);
            }

        }

        // True when every image reports the same camera, Bayer pattern and raw image size
        static bool ImagesFromSameCamera(params HydroColorImageCaptureModel[] images)
        {
            HydroColorImageCaptureModel first = images[0];
            return images.All(image =>
                image.CameraId == first.CameraId
                && image.BayerFilterPattern == first.BayerFilterPattern
                && image.RawImageWidth == first.RawImageWidth
                && image.RawImageHeight == first.RawImageHeight);
        }

        void CancelLocationRequest()
        {
            if (!CanCheckLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
                _cancelTokenSource.Cancel();
        }
    }
}
