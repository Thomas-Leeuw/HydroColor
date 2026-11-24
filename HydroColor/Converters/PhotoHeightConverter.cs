using System.Globalization;

namespace HydroColor.Converters
{
    // The height of a 3 wide by 4 tall photo (the shape of the camera images) for a given width
    public class PhotoHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)value;
            return width > 0 ? width * 4.0 / 3.0 : -1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
