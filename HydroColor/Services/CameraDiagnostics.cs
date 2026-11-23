using System.Globalization;

namespace HydroColor.Services
{
    // Camera diagnostics mode: shows the camera's values after each image and for the whole measurement.
    // It is turned on and off with the "Camera diagnostics" switch in the popup opened by the Camera Diagnostics button at the bottom of the About page.
    // The switch is kept in memory only, so the mode is always off when the app starts.
    public static class CameraDiagnostics
    {
        public static bool Enabled { get; set; }
    }

    // The diagnostic values of one image, as text, in display order.
    // Nothing here is used in any calculation.
    public class CaptureDiagnostics
    {
        public const string NotReported = "not reported";

        public List<KeyValuePair<string, string>> Items { get; } = new();

        // White level as a number, when the phone reports it (for the percent-of-range line)
        public double? WhiteLevel { get; set; }

        // Reads one value and stores it as text. A value the phone does not provide,
        // or any error while reading it, is stored as "not reported".
        public void Add(string label, Func<object> read)
        {
            string text = NotReported;
            try
            {
                object value = read();
                if (value != null)
                {
                    text = ToText(value);
                }
            }
            catch
            {
            }
            Items.Add(new KeyValuePair<string, string>(label, text));
        }

        static string ToText(object value)
        {
            if (value is string s)
            {
                return SingleLine(s);
            }
            if (value is System.Collections.IEnumerable list)
            {
                return string.Join(" ", list.Cast<object>().Select(ToText));
            }
            if (value is double or float)
            {
                // 6 significant digits is more than any of these values is accurate to
                return Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("G6", CultureInfo.InvariantCulture);
            }
            return SingleLine(Convert.ToString(value, CultureInfo.InvariantCulture) ?? NotReported);
        }

        // Replaces line breaks and repeated spaces with a single space
        static string SingleLine(string text)
        {
            return string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }

        // The device, system and app version, for the top of the diagnostics text
        public static string DeviceHeader()
        {
            try
            {
                return $"Device: {DeviceInfo.Manufacturer} {DeviceInfo.Model}\n"
                     + $"System: {DeviceInfo.Platform} {DeviceInfo.VersionString}\n"
                     + $"HydroColor: {AppInfo.Current.VersionString}\n"
                     + $"Time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}";
            }
            catch
            {
                return "Device: " + NotReported;
            }
        }

        // One line per value, for a single image
        public string ToDisplayText()
        {
            return DeviceHeader() + "\n\n" + string.Join("\n", Items.Select(i => $"{i.Key}: {i.Value}"));
        }

        // One line per value, with the three images side by side
        public static string ToSummaryText(CaptureDiagnostics grayCard, CaptureDiagnostics sky, CaptureDiagnostics water)
        {
            string Find(CaptureDiagnostics d, string label) => d?.Items.FirstOrDefault(i => i.Key == label).Value ?? NotReported;

            // A value that is the same in all three images is shown once
            string Row(string label)
            {
                string g = Find(grayCard, label), s = Find(sky, label), w = Find(water, label);
                return g == s && s == w ? $"{label}: {g} (all three)" : $"{label}: {g} | {s} | {w}";
            }

            IEnumerable<string> labels = new[] { grayCard, sky, water }
                .Where(d => d != null)
                .SelectMany(d => d.Items.Select(i => i.Key))
                .Distinct();

            return DeviceHeader() + "\n\n"
                + "Order: gray card | sky | water\n\n"
                + string.Join("\n", labels.Select(Row));
        }
    }
}
