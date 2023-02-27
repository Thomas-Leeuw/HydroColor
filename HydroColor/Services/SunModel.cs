
namespace HydroColor.Services
{
    public class SunAngles
    {
        public double Zenith;
        public double Azimuth;
        public double ElevationAngle;
    }

    public static class SunModel
    {
        // get sun position for current time and location (https://www.pveducation.org/pvcdrom/properties-of-sunlight/the-suns-position)
        public static SunAngles GetSunPosition(DateTime gmtTime, double lat, double lon)
        {

            // convert Lat Lon to radians
            double LatR = lat * (Math.PI / 180);
            double LonR = lon * (Math.PI / 180);

            // Caculate day of year and GMT decimal hour
            double dayOfYear = gmtTime.DayOfYear;
            double hour = gmtTime.Hour + (gmtTime.Minute / 60.0);

            // Caculate the Equation of Time to correct for the eccentricity of the Earth's orbit and the Earth's axial tilt
            double B = 360.0 / 365.0 * (dayOfYear - 81) * (Math.PI / 180); // convert to radians for use with c# trig functions
            double EoT = 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);

            // Time correction factor (TC)
            double TC = 4 * lon + EoT;

            // Calculate local solar time (LST)
            double LST = hour + TC / 60;

            // Check if LST is for the same day as zero degrees longitude (GMT time)
            if ((LST > 24) || (LST < 0))
            {
                LST = 24 - LST;
            }

            // Caculate hour angle (HRA)
            // In the morning the hour angle is negative, in the afternoon the hour angle is positive
            double HRA = 15 * (LST - 12) * (Math.PI / 180); // convert to radians for use with c# trig functions

            // Calculate the declination angle
            double dec = 23.45 * Math.Sin(B) * (Math.PI / 180); // convert to radians for use with c# trig functions

            // Calculate sun elevation
            double SEA = Math.Asin(Math.Sin(dec) * Math.Sin(LatR) + Math.Cos(dec) * Math.Cos(LatR) * Math.Cos(HRA));

            // Calculate sun azimuth
            double azimuth = Math.Acos((Math.Sin(dec) * Math.Cos(LatR) - Math.Cos(HRA) * Math.Cos(dec) * Math.Sin(LatR)) / Math.Cos(SEA));
        
            if (HRA > 0) // in the afternoon, the azimuth angle need to be adjusted (350 - azimuth)
            {
                azimuth = 2 * Math.PI - azimuth;
            }
            
            // Convert SEA to zenith angle
            double elevationAngle = SEA * (180 / Math.PI);
            double zenith = 90 - elevationAngle;
            azimuth *= (180 / Math.PI);

            // Return sun angles in degrees
            SunAngles sunAngles = new()
            {
                Zenith = zenith,
                Azimuth = azimuth,
                ElevationAngle = elevationAngle
            };

            return sunAngles;
        }

    }
}
