using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Converters;
using HydroColor.Models;
using HydroColor.Services;

namespace HydroColor.ViewModels
{
    // What is drawn for one color in the reflectance chart
    public class ReflectanceBar
    {
        public double BarHeight { get; set; }
        public Thickness LabelMargin { get; set; }      // lifts the value text above the bar
        public string Text { get; set; }
    }

    [QueryProperty(nameof(ProcMeas), "MeasurementData")]
    public partial class DataViewModel : ObservableObject
    {
        [ObservableProperty]
        HydroColorProcessedMeasurement procMeas;

        [ObservableProperty]
        ImageSource grayCardThumbnailImage;

        [ObservableProperty]
        ImageSource waterThumbnailImage;

        [ObservableProperty]
        ImageSource skyThumbnailImage;

        [ObservableProperty]
        ReflectanceBar blueBar = new();

        [ObservableProperty]
        ReflectanceBar greenBar = new();

        [ObservableProperty]
        ReflectanceBar redBar = new();

        // Height of the reflectance chart
        [ObservableProperty]
        int barChartHeight = 175;

        [ObservableProperty]
        Color grayCardImageSquareColor = Colors.White;

        [ObservableProperty]
        Color skyImageSquareColor = Colors.White;

        [ObservableProperty]
        Color waterImageSquareColor = Colors.White;

        partial void OnProcMeasChanged(HydroColorProcessedMeasurement value)
        {

            if (ProcMeas.GrayCardImageData.JpegImage != null)
            {
                GrayCardThumbnailImage = ImageSource.FromStream(() => new MemoryStream(ProcMeas.GrayCardImageData.JpegImage));
            }
            if (ProcMeas.SkyImageData.JpegImage != null)
            {
                SkyThumbnailImage = ImageSource.FromStream(() => new MemoryStream(ProcMeas.SkyImageData.JpegImage));
            }
            if (ProcMeas.WaterImageData.JpegImage != null)
            {
                WaterThumbnailImage = ImageSource.FromStream(() => new MemoryStream(ProcMeas.WaterImageData.JpegImage));
            }

            double reflecBlue = Math.Round(ProcMeas.MeasurementProducts.Reflectance.Blue, 3);
            double reflecGreen = Math.Round(ProcMeas.MeasurementProducts.Reflectance.Green, 3);
            double reflecRed = Math.Round(ProcMeas.MeasurementProducts.Reflectance.Red, 3);

            // The chart is scaled so that the tallest bar, with the value text above it, just fits in the chart's height.
            double[] values = { reflecBlue, reflecGreen, reflecRed };
            double[] uncertainties = values.Select(OpticalPropertiesCalculator.ReflectanceUncertainty).ToArray();
            double plotHeight = BarChartHeight - 26;
            double top = values.Max();
            double scale = top > 0 && double.IsFinite(top) ? plotHeight / top : 0;

            BlueBar = MakeBar(values[0], uncertainties[0], scale);
            GreenBar = MakeBar(values[1], uncertainties[1], scale);
            RedBar = MakeBar(values[2], uncertainties[2], scale);

            GrayCardImageSquareColor = ProcMeas.GrayCardImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
            SkyImageSquareColor = ProcMeas.SkyImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
            WaterImageSquareColor = ProcMeas.WaterImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;

            OnPropertyChanged(nameof(TurbidityText));
            OnPropertyChanged(nameof(SPMText));
            OnPropertyChanged(nameof(BackscatterText));
            OnPropertyChanged(nameof(HasDiagnostics));
        }

        // True when the turbidity is at the limit of the calculation, so it and the two results
        // derived from it (SPM and backscatter) are lower limits
        bool AtTurbidityLimit => ProcMeas?.MeasurementProducts.WaterTurbidity >= OpticalPropertiesCalculator.MaxTurbidity;

        // The main values of the three result cards
        public string TurbidityText => ProductText(ProcMeas?.MeasurementProducts.WaterTurbidity, ProductDisplayParameter.TURBIDTY, "F0");
        public string SPMText => ProductText(ProcMeas?.MeasurementProducts.SPM, ProductDisplayParameter.SPM, "F0");
        public string BackscatterText => ProductText(ProcMeas?.MeasurementProducts.Backscatter_red, ProductDisplayParameter.BACKSCATTER, "F2");

        // "value ± uncertainty", or "value+" when the turbidity is at its limit
        string ProductText(double? value, ProductDisplayParameter product, string format)
        {
            if (value == null)
            {
                return "";
            }
            if (AtTurbidityLimit)
            {
                return value.Value.ToString(format) + "+";
            }
            return (string)new ProductStringWithUncertainityConverter().Convert(value.Value, typeof(string), product, System.Globalization.CultureInfo.CurrentCulture);
        }

        // Sizes one bar of the reflectance chart and its value text (shown with its uncertainty).
        // A value of zero or not-a-number gives no bar.
        static ReflectanceBar MakeBar(double value, double uncertainty, double scale)
        {
            double barHeight = double.IsFinite(value) && value > 0 ? value * scale : 0;

            return new ReflectanceBar
            {
                BarHeight = barHeight,
                LabelMargin = new Thickness(0, 0, 0, barHeight + 2),
                Text = $"{value.ToString("F3")} ± {uncertainty}"
            };
        }

        // True when the measurement was just taken in camera diagnostics mode
        public bool HasDiagnostics => ProcMeas?.GrayCardImageData?.Diagnostics != null
            || ProcMeas?.SkyImageData?.Diagnostics != null
            || ProcMeas?.WaterImageData?.Diagnostics != null;

        // Shows the camera values of the three images side by side (diagnostics mode only)
        [RelayCommand]
        async Task ShowDiagnostics()
        {
            try
            {
                string text = CaptureDiagnostics.ToSummaryText(ProcMeas.GrayCardImageData.Diagnostics, ProcMeas.SkyImageData.Diagnostics, ProcMeas.WaterImageData.Diagnostics);
                System.Diagnostics.Debug.WriteLine($"HydroColor diagnostics: summary\n{text}");

                bool copy = await Shell.Current.CurrentPage.DisplayAlertAsync("Camera Diagnostics", text, "Copy", "Close");
                if (copy)
                {
                    await Clipboard.Default.SetTextAsync(text);
                }
            }
            catch
            {
            }
        }

        [RelayCommand]
        public void BackButton()
        {

            // Passing null for the Query Properties is a workaround for a known issue (https://github.com/dotnet/maui/issues/10294)
            // If null is not sent, it will resend the old data again, even if the local parameters have been set to null
            Shell.Current.GoToAsync($"..", true, new Dictionary<string, object>
            {
                [nameof(HydroColorImageTag.GrayCard)] = null,
                [nameof(HydroColorImageTag.Water)] = null,
                [nameof(HydroColorImageTag.Sky)] = null
            });

        }


        }
}
