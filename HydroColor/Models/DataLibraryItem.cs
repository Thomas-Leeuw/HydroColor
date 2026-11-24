using HydroColor.Resources.Strings;
using HydroColor.Services;

namespace HydroColor.Models
{
    public class DataLibraryItem
    {
        public DateTime LocalTimestamp { get; set; }

        public string MeasurementName { get; set; }

        public string WaterImageURI { get; set; }

        // Turbidity of the measurement, for the Library list. Not-a-number when it could not be read.
        public double Turbidity { get; set; } = double.NaN;

        // The turbidity as shown in the Library list, for example "12 NTU". Empty when it could not be read.
        // The calculation stops at a limit, so a value at that limit is shown with a plus sign ("80+ NTU").
        public string TurbidityText => !double.IsFinite(Turbidity) ? ""
            : Turbidity >= OpticalPropertiesCalculator.MaxTurbidity ? $"{Turbidity:F0}+ {Strings.Data_TableTurbidityUnits}"
            : $"{Turbidity:F0} {Strings.Data_TableTurbidityUnits}";

    }
}
