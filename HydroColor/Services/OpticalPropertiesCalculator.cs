using HydroColor.Models;

namespace HydroColor.Services
{
    public static class OpticalPropertiesCalculator
    {
        const double GrayCardReflectance = 0.18; // Assumes user is using 18% reflectance photographers gray card
        const double WaterSurfaceReflectanceFactor = 0.028; // From Mobley 1999 for viewing angle 135 deg from sun and 40 degrees from nadir, no wind.

        public static HydroColorMeasurementProducts ComputeOpticalProperties(HydroColorImageCaptureModel GrayCardImageData,
            HydroColorImageCaptureModel SkyImageData,
            HydroColorImageCaptureModel WaterImageData)
        {

            HydroColorMeasurementProducts Products = new();

            Products.GrayCardRelativeLightLevel = ComputeRelativeLightLevel(GrayCardImageData);
            Products.SkyRelativeLightLevel =      ComputeRelativeLightLevel(SkyImageData);
            Products.WaterRelativeLightLevel =    ComputeRelativeLightLevel(WaterImageData);

            Products.Reflectance.Red =   ComputeReflectance(Products.GrayCardRelativeLightLevel.Red,
                                                            Products.SkyRelativeLightLevel.Red,
                                                            Products.WaterRelativeLightLevel.Red);

            Products.Reflectance.Green = ComputeReflectance(Products.GrayCardRelativeLightLevel.Green,
                                                            Products.SkyRelativeLightLevel.Green,
                                                            Products.WaterRelativeLightLevel.Green);

            Products.Reflectance.Blue =  ComputeReflectance(Products.GrayCardRelativeLightLevel.Blue,
                                                            Products.SkyRelativeLightLevel.Blue,
                                                            Products.WaterRelativeLightLevel.Blue);

            Products.WaterTurbidity = ComputeTurbidity(Products.Reflectance.Red);
            Products.SPM = ComputeSPMFromTurbidity(Products.WaterTurbidity);
            Products.Backscatter_red = ComputeRedBackscatter(Products.Reflectance.Red, Products.SPM);

            return Products;
        }

        public static ColorChannelData<double> ComputeRelativeLightLevel(HydroColorImageCaptureModel ImageData)
        {
            ColorChannelData<double> RelativeLightLevel = new();

            // From Leeuw and Boss 2018
            RelativeLightLevel.Red =   (ImageData.MedianPixelValue.Red   - ImageData.BlackLevel.Red)   / (ImageData.ExposureTime * ImageData.SensorSensitivity);
            RelativeLightLevel.Green = (ImageData.MedianPixelValue.Green - ImageData.BlackLevel.Green) / (ImageData.ExposureTime * ImageData.SensorSensitivity);
            RelativeLightLevel.Blue =  (ImageData.MedianPixelValue.Blue  - ImageData.BlackLevel.Blue)  / (ImageData.ExposureTime * ImageData.SensorSensitivity);

            RelativeLightLevel.Red   = RelativeLightLevel.Red   >= 0 ? RelativeLightLevel.Red   : 0;
            RelativeLightLevel.Green = RelativeLightLevel.Green >= 0 ? RelativeLightLevel.Green : 0;
            RelativeLightLevel.Blue  = RelativeLightLevel.Blue  >= 0 ? RelativeLightLevel.Blue  : 0;

            return RelativeLightLevel;
        }

        public static double ComputeReflectance(double GrayCardLightLevel, double SkyLightLevel, double WaterLightLevel)
        {
            // From Mobley 1999

            // relative, uncalibrated radiance/irradiance
            double WaterLeavingRadiance = WaterLightLevel - (WaterSurfaceReflectanceFactor * SkyLightLevel);
            double DownwellingIrradiance = (Math.PI / GrayCardReflectance) * GrayCardLightLevel;

            if (DownwellingIrradiance > 0)
            {
                // ratio cancels any scaling factor
                double reflectance = WaterLeavingRadiance / DownwellingIrradiance;

                reflectance = Math.Round(reflectance, 4);
                return reflectance >= 0 ? reflectance : 0;
            }
            else
            {
                return 0;
            }
        }

        public static double ComputeTurbidity(double RedReflectance)
        {
            double Turbidity;

            if (RedReflectance >= 0.0372)
            {
                // Red reflectace approches asymptote for trubidity over 80 NTU or Rrs over 0.0372
                Turbidity = 80;
            }
            else
            {
                // From Leeuw and Boss 2018
                Turbidity = (27.7 * RedReflectance) / (0.05 - RedReflectance);
            }

            return Math.Round(Turbidity,0);
        }

        public static double ComputeSPMFromTurbidity(double turbidity)
        {
            // From Neukermans et al. 2012
            double SPM = Math.Pow(10, (1.02 * Math.Log10(turbidity) - 0.04));
            return Math.Round(SPM, 0);
        }

        public static double ComputeRedBackscatter(double ReflectanceRed, double SPM)
        {
            double rrs_sub = ReflectanceRed / (0.5 + 1.5 * ReflectanceRed); // propagate below the surface (Lee et al. 1999)
            double u = (-0.0949 + Math.Sqrt(Math.Pow(0.0949, 2) + 4* 0.0794 * rrs_sub)) / (2 * 0.0794); // solve quadratic equation for bb/(bb+a) (Gordon 1988)

            // solve for bb (using weight integrated a_w and ap given by 0.012*exp(-.008*(wl-550))
            double bb_red = (u * (0.2479 + SPM * 0.0085)) / (-u + 1);

            return Math.Round(bb_red,2);
        }

        public static double ReflectanceUncertainty(double reflectance)
        {
            double uncer;
            if (reflectance < 0.008)
            {
                uncer = 0.001; // based on mean abs error for all channels (in comparision to WISP) - Leeuw and Boss 2018
            }
            else
            {
                uncer = reflectance * 0.15; // based on mean abs % error for all channels (in comparision to WISP)
            }

            return Math.Round(uncer, 3);
        }

        public static double TurbidityUncertainty(double turbidity)
        {
            double uncer;
            if (turbidity < 3)
            {
                uncer = 1; // based on mean abs error for tur < 5 - Leeuw and Boss 2018
            }
            else
            {
                uncer = turbidity * 0.36; // mean abs % error for all turbidity data
            }

            return Math.Round(uncer, 0);
        }

        public static double SPMUncertainty(double SPM)
        {
            double uncer;
            if (SPM < 3)
            {
                uncer = 1; //based on uncertainty in turbidity 
            }
            else
            {
                uncer = SPM * 0.38; // rme of after Neukermans conversion to spm
            }

            return Math.Round(uncer, 0);
        }

        public static double BackscatterRedUncertainty(double BackscatterRed)
        {
            double uncer;
            if (BackscatterRed < 0.05)
            {
                uncer = 0.02; // based on uncertinity at 3 NTU
            }
            else
            {
                uncer = BackscatterRed * 0.41; // based on rme of rrs and spm
            }

            return Math.Round(uncer, 2);
        }

    }
}
