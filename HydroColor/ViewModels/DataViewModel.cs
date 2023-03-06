using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;

namespace HydroColor.ViewModels
{
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
        int blueBarChartHeight;

        [ObservableProperty]
        int greenBarChartHeight;

        [ObservableProperty]
        int redBarChartHeight;

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


            double maxReflec = new[] {  reflecBlue,
                                        reflecGreen,
                                        reflecRed
                                        }.Max();

            BlueBarChartHeight = (int) (reflecBlue / maxReflec * (BarChartHeight - 30));
            GreenBarChartHeight = (int) (reflecGreen / maxReflec * (BarChartHeight - 30));
            RedBarChartHeight = (int) (reflecRed / maxReflec * (BarChartHeight - 30));

            GrayCardImageSquareColor = ProcMeas.GrayCardImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
            SkyImageSquareColor = ProcMeas.SkyImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
            WaterImageSquareColor = ProcMeas.WaterImageData.ImageCapturedAtCorrectAngles ? Colors.LimeGreen : Colors.White;
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
