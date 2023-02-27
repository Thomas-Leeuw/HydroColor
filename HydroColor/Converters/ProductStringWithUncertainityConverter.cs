using HydroColor.Services;
using System.Globalization;


namespace HydroColor.Converters
{
    public enum ProductDisplayParameter
    {
        REFLECTANCE,
        TURBIDTY,
        SPM,
        BACKSCATTER
    }

    public class ProductStringWithUncertainityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            ProductDisplayParameter product = (ProductDisplayParameter) parameter;
            double productValue = (double)value;

            double uncertainity = 0;
            int decmialPrecision = 0;
            switch (product)
            {
                case ProductDisplayParameter.REFLECTANCE:
                    uncertainity = OpticalPropertiesCalculator.ReflectanceUncertainty(productValue);
                    decmialPrecision = 3;
                    break;
                case ProductDisplayParameter.TURBIDTY:
                    uncertainity = OpticalPropertiesCalculator.TurbidityUncertainty(productValue);
                    decmialPrecision = 0;
                    break;
                case ProductDisplayParameter.SPM:
                    uncertainity = OpticalPropertiesCalculator.SPMUncertainty(productValue);
                    decmialPrecision = 0;
                    break;
                case ProductDisplayParameter.BACKSCATTER:
                    uncertainity = OpticalPropertiesCalculator.BackscatterRedUncertainty(productValue);
                    decmialPrecision = 2;
                    break;
            }

           
            return $"{Math.Round(productValue,decmialPrecision).ToString($"F{decmialPrecision}")} ± {uncertainity}";

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
