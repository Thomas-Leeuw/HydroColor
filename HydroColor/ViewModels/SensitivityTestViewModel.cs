using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
using HydroColor.Services;
using System.Globalization;
using System.Text;
using static Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using CameraController = HydroColor.Platforms.Android.CameraController;
#elif IOS
using CameraController = HydroColor.Platforms.iOS.CameraController;
#endif

namespace HydroColor.ViewModels
{
    // Sensitivity test (camera diagnostics mode only). Takes a series of raw images of a steady, evenly lit
    // target with the ISO and exposure time set by the app, and reports whether the computed light level
    // stays the same. Each image is processed with the same functions as a measurement.
    public partial class SensitivityTestViewModel : ObservableObject
    {
        const string Instructions = "Fill the square with a steady, evenly lit target (a gray card in daylight or open shade, not under mains-powered lamps). Keep the phone still, then press Start.";

        [ObservableProperty]
        bool cameraPreviewLayoutFinished;

        [ObservableProperty]
        CameraController camControl = new CameraController() { KeepRunningAfterCapture = true };

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        bool isRunning;

        [ObservableProperty]
        string statusText = Instructions;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
        string resultsText = "";

        bool _pageActive;
        bool _openingCamera;
        CancellationTokenSource _cancel;
        TaskCompletionSource<ImageCaptureEventArgs> _pendingCapture;

        // One captured image of the test
        class TestStep
        {
            public string Label;
            public double RequestedISO = double.NaN;
            public double RequestedSeconds = double.NaN;
            public double UsedSensitivity;
            public double UsedSeconds;
            public double GreenCounts;
            public double PercentOfRange = double.NaN; // brightest color
            public double LightLevel;                  // green
            public string Extra = "";
        }

        public SensitivityTestViewModel()
        {
            CamControl.ImageCaptureCompletedEvent += (sender, e) => _pendingCapture?.TrySetResult(e);
        }

        async partial void OnCameraPreviewLayoutFinishedChanged(bool value)
        {
            if (!value)
            {
                // The preview surface was destroyed: the app went to the background or the page is closing.
                _cancel?.Cancel();
                CamControl.CloseCamera();
                return;
            }

            await OpenCameraPreview();
        }

        // Called when the app returns to the foreground
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
            if (CamControl.IsOpen || _openingCamera)
            {
                return;
            }
            _openingCamera = true;

            try
            {
                PermissionStatus permissionStatus = await HardwarePermissions.RequestPermission<Camera>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    await Shell.Current.GoToAsync("..", true);
                    return;
                }

                CamControl.OpenCamera();
            }
            catch (Exception ex)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync("Camera Error", ex.Message, "OK");
                await Shell.Current.GoToAsync("..", true);
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
            Shell.Current.Window.Resumed -= Window_Resumed;
            Shell.Current.Window.Resumed += Window_Resumed;
        }

        [RelayCommand]
        void ViewUnloaded()
        {
            _pageActive = false;
            Shell.Current.Window.Resumed -= Window_Resumed;
            _cancel?.Cancel();
            try
            {
                CamControl.RestoreAutoExposure();
            }
            catch
            {
            }
            CamControl.CloseCamera();
        }

        bool CanStart => !IsRunning;
        bool CanCancel => IsRunning;
        bool CanCopy => !string.IsNullOrEmpty(ResultsText);

        [RelayCommand(CanExecute = nameof(CanCancel))]
        void Cancel()
        {
            _cancel?.Cancel();
        }

        [RelayCommand(CanExecute = nameof(CanCopy))]
        async Task Copy()
        {
            try
            {
                await Clipboard.Default.SetTextAsync(ResultsText);
            }
            catch
            {
            }
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        async Task Start()
        {
            IsRunning = true;
            ResultsText = "";
            _cancel = new CancellationTokenSource();
            CancellationToken ct = _cancel.Token;

            List<TestStep> steps = new();
            string rangeNote = "";
            double? analogISOLimit = null;

            try
            {
                ManualExposureLimits limits = CamControl.GetManualExposureLimits();
                analogISOLimit = limits.MaxAnalogISO;
                if (!limits.Supported)
                {
                    StatusText = "This phone does not let the app set the ISO and exposure time, so the test cannot run.";
                    return;
                }

                // Reference image with automatic exposure, to find how bright the target is
                StatusText = "Measuring the target...";
                CamControl.RestoreAutoExposure();
                await Task.Delay(1500, ct);
                TestStep reference = await CaptureStep("AUTO", null, null, ct);
                steps.Add(reference);

                // ISO x exposure time that puts the brightest color at about 30% of the sensor range
                double product = reference.UsedSensitivity * reference.UsedSeconds;
                if (reference.PercentOfRange > 0)
                {
                    product *= Math.Clamp(30 / reference.PercentOfRange, 0.1, 10);
                }

                double minSeconds = limits.MinExposureSeconds;
                double maxSeconds = Math.Min(limits.MaxExposureSeconds, 0.25);
                double isoLow = Math.Max(limits.MinISO, product / maxSeconds);
                double isoHigh = Math.Min(limits.MaxISO, product / minSeconds);
                if (!(isoHigh > isoLow))
                {
                    StatusText = "The target is too bright or too dark for this test.";
                    return;
                }
                if (isoLow > limits.MinISO * 1.01 || isoHigh < limits.MaxISO * 0.99)
                {
                    rangeNote = string.Format(CultureInfo.InvariantCulture,
                        "In this light the sweep covers ISO {0:F0} to {1:F0} of the phone's {2:F0} to {3:F0}.",
                        isoLow, isoHigh, limits.MinISO, limits.MaxISO);
                }

                // ISO sweep: ISO x exposure time is kept constant, so the signal should stay about the same
                List<double> isoValues = new();
                for (int i = 0; i < 10; i++)
                {
                    isoValues.Add(isoLow * Math.Pow(isoHigh / isoLow, i / 9.0));
                }
                if (limits.MaxAnalogISO is double analogLimit && analogLimit > isoLow && analogLimit < isoHigh)
                {
                    isoValues.Add(analogLimit);
                    isoValues.Sort();
                }

                for (int i = 0; i < isoValues.Count; i++)
                {
                    StatusText = $"ISO sweep: image {i + 1} of {isoValues.Count}";
                    steps.Add(await CaptureStep("ISO", isoValues[i], product / isoValues[i], ct));
                }

                // Exposure sweep: ISO is fixed and the exposure time is stepped
                double fixedISO = Math.Max(isoLow, 2.5 * product / maxSeconds);
                if (fixedISO <= isoHigh)
                {
                    double[] factors = { 0.17, 0.33, 0.67, 1, 1.5, 2.5 };
                    for (int i = 0; i < factors.Length; i++)
                    {
                        double seconds = factors[i] * product / fixedISO;
                        if (seconds < minSeconds || seconds > maxSeconds)
                        {
                            continue;
                        }
                        StatusText = $"Exposure sweep: image {i + 1} of {factors.Length}";
                        steps.Add(await CaptureStep("EXP", fixedISO, seconds, ct));
                    }
                }

                // The first ISO step again, to see whether the light changed during the test
                StatusText = "Repeating the first image...";
                steps.Add(await CaptureStep("REPEAT", isoValues[0], product / isoValues[0], ct));

                StatusText = "Test complete. " + Instructions;
            }
            catch (OperationCanceledException)
            {
                StatusText = "Test cancelled.";
            }
            catch (Exception ex)
            {
                StatusText = "Test stopped: " + ex.Message;
            }
            finally
            {
                try
                {
                    CamControl.RestoreAutoExposure();
                }
                catch
                {
                }

                if (steps.Count > 0)
                {
                    ResultsText = BuildResults(steps, rangeNote, analogISOLimit);
                    System.Diagnostics.Debug.WriteLine("HydroColor sensitivity test\n" + ResultsText);
                }
                IsRunning = false;
            }
        }

        // Takes one image (automatic exposure when iso is null) and reduces it to the values for the table.
        // Stops the test if the camera does not answer within 10 seconds.
        async Task<TestStep> CaptureStep(string label, double? iso, double? seconds, CancellationToken ct)
        {
            await Task.Delay(150, ct);

            TaskCompletionSource<ImageCaptureEventArgs> capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingCapture = capture;

            if (iso == null)
            {
                CamControl.TakeRAWImage();
            }
            else
            {
                CamControl.TakeRAWImage(iso.Value, seconds.Value);
            }

            Task finished = await Task.WhenAny(capture.Task, Task.Delay(TimeSpan.FromSeconds(10), ct));
            ct.ThrowIfCancellationRequested();
            if (finished != capture.Task)
            {
                throw new TimeoutException("the camera did not return an image.");
            }

            ImageCaptureEventArgs e = await capture.Task;
            if (e.ImageData == null)
            {
                throw new InvalidOperationException(e.ErrorMessage ?? "the capture failed.");
            }

            return await Task.Run(() =>
            {
                HydroColorRawImageData raw = e.ImageData;

                // The same processing as a measurement image
                HydroColorImageCaptureModel image = new()
                {
                    MedianPixelValue = BayerPatternDemosaic.GetMedianPixelValues(raw),
                    BlackLevel = raw.BlackLevel,
                    ExposureTime = raw.ExposureTime,
                    SensorSensitivity = raw.SensorSensitivity
                };
                ColorChannelData<double> lightLevel = OpticalPropertiesCalculator.ComputeRelativeLightLevel(image);

                TestStep step = new()
                {
                    Label = label,
                    RequestedISO = iso ?? double.NaN,
                    RequestedSeconds = seconds ?? double.NaN,
                    UsedSensitivity = raw.SensorSensitivity,
                    UsedSeconds = raw.ExposureTime,
                    GreenCounts = image.MedianPixelValue.Green - image.BlackLevel.Green,
                    LightLevel = lightLevel.Green
                };

                CaptureDiagnostics d = raw.Diagnostics;
                if (d?.WhiteLevel is double white)
                {
                    step.PercentOfRange = 100 * new[]
                    {
                        (image.MedianPixelValue.Red - image.BlackLevel.Red) / (white - image.BlackLevel.Red),
                        (image.MedianPixelValue.Green - image.BlackLevel.Green) / (white - image.BlackLevel.Green),
                        (image.MedianPixelValue.Blue - image.BlackLevel.Blue) / (white - image.BlackLevel.Blue)
                    }.Max();
                }

                string Find(string name) => d?.Items.FirstOrDefault(i => i.Key == name).Value ?? "?";
#if IOS
                step.Extra = $"camera ISO {Find("USED camera ISO")}, baseline exposure {Find("USED baseline exposure")}, camera exposure {Find("Exposure time, camera (s)")} s";
#elif ANDROID
                step.Extra = $"post-raw boost {Find("Post-raw boost")}, black level this capture {Find("Black level, this capture")}";
#endif

                raw.RawImagePixelData = null; // release the large raw image
                return step;
            });
        }

        static string BuildResults(List<TestStep> steps, string rangeNote, double? analogISOLimit)
        {
            CultureInfo inv = CultureInfo.InvariantCulture;

            // Deviations are relative to the first step of the ISO sweep (or the automatic image if there is none)
            TestStep reference = steps.FirstOrDefault(s => s.Label == "ISO") ?? steps[0];
            double Deviation(TestStep s) => 100 * (s.LightLevel / reference.LightLevel - 1);

            StringBuilder text = new();
            text.AppendLine(CaptureDiagnostics.DeviceHeader());
            text.AppendLine();
            if (rangeNote != "")
            {
                text.AppendLine(rangeNote);
                text.AppendLine();
            }

            // The highest ISO the sensor reaches with its own (analog) gain, when the phone reports it
            text.AppendLine(analogISOLimit is double limit
                ? string.Format(inv, "Hardware (analog) ISO limit: {0:F0}. Above it the phone adds gain by other means.", limit)
                : "Hardware (analog) ISO limit: not reported by this phone.");
            text.AppendLine();

            text.AppendLine("Step    Set ISO   Set ms  Used sens  Used ms  Green  %range  Light level  Dev %  Notes");
            foreach (TestStep s in steps)
            {
                string flag = s.PercentOfRange < 1 ? "LOW SIGNAL. " : s.PercentOfRange > 90 ? "NEAR SATURATION. " : "";
                if (analogISOLimit is double hardwareLimit && s.RequestedISO > hardwareLimit * 1.001)
                {
                    flag = "ABOVE HARDWARE ISO LIMIT. " + flag;
                }
                text.AppendLine(string.Format(inv, "{0,-6} {1,8:F0} {2,8:F3} {3,10:F1} {4,8:F3} {5,6:F0} {6,7:F1} {7,12:G5} {8,6:+0.0;-0.0}  {9}{10}",
                    s.Label, s.RequestedISO, s.RequestedSeconds * 1000, s.UsedSensitivity, s.UsedSeconds * 1000,
                    s.GreenCounts, s.PercentOfRange, s.LightLevel, Deviation(s), flag, s.Extra));
            }

            List<TestStep> sweep = steps.Where(s => (s.Label == "ISO" || s.Label == "EXP") && !(s.PercentOfRange < 1) && !(s.PercentOfRange > 90)).ToList();
            if (sweep.Count > 0)
            {
                TestStep worst = sweep.OrderByDescending(s => Math.Abs(Deviation(s))).First();
                text.AppendLine();
                text.AppendLine(string.Format(inv, "Largest deviation: {0:+0.0;-0.0}% ({1} step, ISO {2:F0}, {3:F3} ms)",
                    Deviation(worst), worst.Label, worst.RequestedISO, worst.RequestedSeconds * 1000));
            }

            TestStep repeat = steps.FirstOrDefault(s => s.Label == "REPEAT");
            if (repeat != null)
            {
                text.AppendLine(string.Format(inv, "Repeat of the first step: {0:+0.0;-0.0}%{1}",
                    Deviation(repeat), Math.Abs(Deviation(repeat)) > 2 ? " (the light changed during the test; run it again)" : " (the light was steady)"));
            }

            return text.ToString();
        }
    }
}
