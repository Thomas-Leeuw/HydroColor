using HydroColor.Models;
using MimeKit;
using System.Globalization;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using HydroColor.Resources.Strings;
using CommunityToolkit.Maui.Views;
using HydroColor.Views;
using System.Linq.Expressions;
using CommunityToolkit.Maui.Extensions;

namespace HydroColor.Services
{
    public class FileReaderWriter
    {

        const string DataFileName = "HydroColor_Datafile.txt";

        public string DataFolderPath
        {
#if ANDROID
            get { return Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath; }      
#elif IOS
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }      
#endif
        }

        string EmailSenderName = "HydroColor App";
        string EmailReceiverName = "HydroColor User";
        string EmailSubject = "HydroColor Data";
        string EmailBody = @"Dear HydroColor User,

Your HydroColor data file is attached. Do not reply to this email.";

        public string GetDataFileName()
        {
            return Path.Combine(DataFolderPath, DataFileName);
        }

        // ---------------------------------------------------------------------------------
        // Data file format
        //
        // The data file is always written with the invariant culture, so numbers and dates
        // look the same on every phone regardless of its language and region settings
        // (for example 2026/11/22, 13:04:05 and 0.0123).
        //
        // Earlier versions wrote with the phone's own culture. When reading, the invariant
        // format is tried first and the phone's own format second, so those records still open.
        // ---------------------------------------------------------------------------------

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        const string DateFormat = "yyyy/MM/dd";
        const string TimeFormat = "HH:mm:ss";
        const string ImageTimestampFormat = "yyyyMMdd_HHmmss";

        const int RecordFieldCount = 45;   // columns in a record written by version 2.1 or later
        const int MinimumFieldCount = 44;  // columns in a record written by version 2.0

        // The header line is the list of column names, which starts with "Date"
        static bool IsHeaderLine(string line)
        {
            return line.StartsWith("Date ", StringComparison.Ordinal);
        }

        // Reads a number written in the invariant format or, for records saved by earlier versions,
        // in the phone's own format. Returns 'fallback' if neither works.
        static double ParseNumber(string text, double fallback = double.NaN)
        {
            // NumberStyles.Float does not allow a thousands separator, so "0,0123" is not misread as 123
            if (double.TryParse(text, NumberStyles.Float, Inv, out double value))
            {
                return value;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }
            return fallback;
        }

        static bool ParseBool(string text)
        {
            return bool.TryParse(text, out bool value) && value;
        }

        static BayerFilterType ParseBayerPattern(string text)
        {
            return Enum.TryParse(text, out BayerFilterType value) ? value : BayerFilterType.Unknown;
        }

        // Reads the date and time columns of a record.
        static bool TryParseTimestamp(string date, string time, out DateTime timestamp)
        {
            string text = date + " " + time;
            string format = DateFormat + " " + TimeFormat;

            bool ok = DateTime.TryParseExact(text, format, Inv, DateTimeStyles.None, out timestamp);

            // If that failed, or gave a date in the future (the record was written with a
            // non-Gregorian calendar, such as the Thai Buddhist year), read it the way an
            // earlier version would have written it on this phone.
            if (!ok || timestamp > DateTime.Now.AddDays(1))
            {
                if (DateTime.TryParseExact(text, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime local))
                {
                    timestamp = local;
                    ok = true;
                }
            }

            return ok;
        }

        // Returns the columns of the record with this timestamp, or null if there is no such
        // record or it has fewer than 'minimumFields' columns.
        string[] FindRecord(DateTime timestamp, int minimumFields)
        {
            string DataFilePath = GetDataFileName();

            if (!File.Exists(DataFilePath))
            {
                return null;
            }

            foreach (string line in File.ReadLines(DataFilePath))
            {
                string[] data = line.Split(' ');
                if (data.Length > 1 && TryParseTimestamp(data[0], data[1], out DateTime lineTimestamp) && lineTimestamp == timestamp)
                {
                    return data.Length >= minimumFields ? data : null;
                }
            }

            return null;
        }

        // Path used when saving a new thumbnail image
        string NewImagePath(DateTime timestamp, string imageName)
        {
            return Path.Combine(DataFolderPath, $"{timestamp.ToString(ImageTimestampFormat, Inv)}_{imageName}.jpg");
        }

        // Path of an existing thumbnail image. Earlier versions named the file using the phone's
        // own calendar, so that name is used if the invariant one does not exist.
        string GetImagePath(DateTime timestamp, string imageName)
        {
            string path = NewImagePath(timestamp, imageName);
            if (!File.Exists(path))
            {
                string olderPath = Path.Combine(DataFolderPath, $"{timestamp.ToString(ImageTimestampFormat)}_{imageName}.jpg");
                if (File.Exists(olderPath))
                {
                    return olderPath;
                }
            }
            return path;
        }

        public void WriteDataRecord(HydroColorProcessedMeasurement ProcMeas)
        {

            // All thumbnail image file names use the water image timestamp
            DateTime timestamp = ProcMeas.WaterImageData.LocalTimeStamp;
            File.WriteAllBytes(NewImagePath(timestamp, "GrayCard"), ProcMeas.GrayCardImageData.JpegImage);
            File.WriteAllBytes(NewImagePath(timestamp, "Sky"), ProcMeas.SkyImageData.JpegImage);
            File.WriteAllBytes(NewImagePath(timestamp, "Water"), ProcMeas.WaterImageData.JpegImage);

            WriteTextDataRecord(ProcMeas, DataFileName);
        }

        public void WriteTextDataRecord(HydroColorProcessedMeasurement ProcMeas, string filename)
        {

            string measurementName = ProcMeas.MeasurementName.Replace(" ", "_");
            if (measurementName.Length == 0)
            {
                measurementName = "_";
            }

            Dictionary<string, string> DataRecord = new Dictionary<string, string>
            {
                { "Date",                        ProcMeas.WaterImageData.LocalTimeStamp.ToString(DateFormat, Inv) },
                { "Time",                        ProcMeas.WaterImageData.LocalTimeStamp.ToString(TimeFormat, Inv) },
                { "UTC_Offset",                  ProcMeas.WaterImageData.UTCOffset.ToString("F1", Inv) },
                { "Name",                        measurementName },    // data file is space delimted, replace spacing in measurement name with underscore
                { "HydroColor_Version",          ProcMeas.HydroColorVersion },

                { "Latitude",                    ProcMeas.WaterImageData.ImageLocation.Latitude.ToString("F5", Inv) },
                { "Longitude",                   ProcMeas.WaterImageData.ImageLocation.Longitude.ToString("F5", Inv) },
                { "GPS_Accuracy_meters",         (ProcMeas.WaterImageData.ImageLocation.Accuracy ?? 0).ToString("F1", Inv) },

                { "Reflectance_Red_1/sr",        ProcMeas.MeasurementProducts.Reflectance.Red.ToString("F4", Inv) },
                { "Reflectance_Green_1/sr",      ProcMeas.MeasurementProducts.Reflectance.Green.ToString("F4", Inv) },
                { "Reflectance_Blue_1/sr",       ProcMeas.MeasurementProducts.Reflectance.Blue.ToString("F4", Inv) },
                { "Turbidity_NTU",               ProcMeas.MeasurementProducts.WaterTurbidity.ToString("F0", Inv) },
                { "SPM_mg/L",                    ProcMeas.MeasurementProducts.SPM.ToString("F0", Inv)},
                { "Backscatter_Red_1/m",         ProcMeas.MeasurementProducts.Backscatter_red.ToString("F2", Inv)},

                { "Sun_Elevation_deg",           ProcMeas.WaterImageData.SunElevationAngle.ToString("F0", Inv) },
                { "Sun_Azimuth_deg",             ProcMeas.WaterImageData.SunAzimuthAngle.ToString("F0", Inv) },
                { "Magnetic_Declination_deg",    ProcMeas.WaterImageData.MagneticDeclination.ToString("F1", Inv) },

                { "Device_Manufacturer",         ProcMeas.DeviceManufacturer.Replace(" ", "_") },
                { "Device_Model",                ProcMeas.DeviceModel.Replace(" ", "_") },
                { "Device_OS_Version",           ProcMeas.DeviceOSVersion.Replace(" ", "_") },
                { "Device_Bayer_Pattern",        ProcMeas.WaterImageData.BayerFilterPattern.ToString() },

                { "GrayCard_Capture_Angles_Correct", ProcMeas.GrayCardImageData.ImageCapturedAtCorrectAngles.ToString() },
                { "GrayCard_Heading_deg",        ProcMeas.GrayCardImageData.ImageAzimuthAngle.ToString("F0", Inv) },
                { "GrayCard_Pitch_deg",          ProcMeas.GrayCardImageData.ImageOffNadirAngle.ToString("F0", Inv) },
                { "GrayCard_Exposure_sec",       ProcMeas.GrayCardImageData.ExposureTime.ToString("F6", Inv) },
                { "GrayCard_Sensor_Sensitivity", ProcMeas.GrayCardImageData.SensorSensitivity.ToString("F2", Inv) },
                { "GrayCard_Red_Light_Level",    ProcMeas.MeasurementProducts.GrayCardRelativeLightLevel.Red.ToString("F5", Inv) },
                { "GrayCard_Green_Light_Level",  ProcMeas.MeasurementProducts.GrayCardRelativeLightLevel.Green.ToString("F5", Inv) },
                { "GrayCard_Blue_Light_Level",   ProcMeas.MeasurementProducts.GrayCardRelativeLightLevel.Blue.ToString("F5", Inv) },

                { "Sky_Capture_Angles_Correct",  ProcMeas.SkyImageData.ImageCapturedAtCorrectAngles.ToString() },
                { "Sky_Heading_deg",             ProcMeas.SkyImageData.ImageAzimuthAngle.ToString("F0", Inv) },
                { "Sky_Pitch_deg",               ProcMeas.SkyImageData.ImageOffNadirAngle.ToString("F0", Inv) },
                { "Sky_Exposure_sec",            ProcMeas.SkyImageData.ExposureTime.ToString("F6", Inv) },
                { "Sky_Sensor_Sensitivity",      ProcMeas.SkyImageData.SensorSensitivity.ToString("F2", Inv) },
                { "Sky_Red_Light_Level",         ProcMeas.MeasurementProducts.SkyRelativeLightLevel.Red.ToString("F5", Inv) },
                { "Sky_Green_Light_Level",       ProcMeas.MeasurementProducts.SkyRelativeLightLevel.Green.ToString("F5", Inv) },
                { "Sky_Blue_Light_Level",        ProcMeas.MeasurementProducts.SkyRelativeLightLevel.Blue.ToString("F5", Inv) },

                { "Water_Capture_Angles_Correct",ProcMeas.WaterImageData.ImageCapturedAtCorrectAngles.ToString() },
                { "Water_Heading_deg",           ProcMeas.WaterImageData.ImageAzimuthAngle.ToString("F0", Inv) },
                { "Water_Pitch_deg",             ProcMeas.WaterImageData.ImageOffNadirAngle.ToString("F0", Inv) },
                { "Water_Exposure_sec",          ProcMeas.WaterImageData.ExposureTime.ToString("F6", Inv) },
                { "Water_Sensor_Sensitivity",    ProcMeas.WaterImageData.SensorSensitivity.ToString("F2", Inv) },
                { "Water_Red_Light_Level",       ProcMeas.MeasurementProducts.WaterRelativeLightLevel.Red.ToString("F5", Inv) },
                { "Water_Green_Light_Level",     ProcMeas.MeasurementProducts.WaterRelativeLightLevel.Green.ToString("F5", Inv) },
                { "Water_Blue_Light_Level",      ProcMeas.MeasurementProducts.WaterRelativeLightLevel.Blue.ToString("F5", Inv) }
            };


            string CompleteDataRecord = string.Join(" ", DataRecord.Values.ToList()); // space delimited data record

            string DataFilePath = Path.Combine(DataFolderPath, filename);

            bool WriteHeader = false;
            if (!File.Exists(DataFilePath))
            {
                WriteHeader = true;
            }

            using (StreamWriter w = File.AppendText(DataFilePath))
            {
                if (WriteHeader)
                {
                    w.WriteLine(string.Join(" ", DataRecord.Keys.ToList()));
                }
                w.WriteLine(CompleteDataRecord);
            }
        }

        public void DeleteDataFile()
        {
            if (DataFileExists())
            {
                File.Delete(GetDataFileName());
            }
        }

        public bool DataFileExists()
        {
            return File.Exists(GetDataFileName());
        }

        public List<DataLibraryItem> LoadDataSummary()
        {
            return LoadDataSummary(out _);
        }

        // unreadableCount is the number of lines in the data file that could not be read.
        // Those lines are left in the file but are not returned in the list.
        public List<DataLibraryItem> LoadDataSummary(out int unreadableCount)
        {
            List<DataLibraryItem> LibraryList = new();
            unreadableCount = 0;

            string DataFilePath = GetDataFileName();

            if (!File.Exists(DataFilePath))
            {
                return LibraryList;
            }

            bool firstLine = true;
            foreach (string line in System.IO.File.ReadLines(DataFilePath))
            {
                bool isHeader = firstLine && IsHeaderLine(line);
                firstLine = false;

                if (isHeader || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] dataRecord = line.Split(' ');

                // A line must have every column (a cut-off line does not) and a readable date and time
                if (dataRecord.Length < MinimumFieldCount || !TryParseTimestamp(dataRecord[0], dataRecord[1], out DateTime timestamp))
                {
                    unreadableCount++;
                    continue;
                }

                DataLibraryItem DataRecordItem = new();

                DataRecordItem.LocalTimestamp = timestamp;
                DataRecordItem.MeasurementName = dataRecord[3].Replace('_', ' ');
                DataRecordItem.WaterImageURI = new Uri(GetImagePath(timestamp, "Water")).AbsolutePath;

                // Turbidity is the twelfth value on the line. A value that cannot be read is left as not-a-number.
                DataRecordItem.Turbidity = dataRecord.Length > 11 ? ParseNumber(dataRecord[11]) : double.NaN;

                LibraryList.Add(DataRecordItem);
            }

            return LibraryList;

        }


        public HydroColorProcessedMeasurement GetDataRecord(DateTime timestamp)
        {
            // Returns null if the record is missing or does not have every column
            string[] Data = FindRecord(timestamp, RecordFieldCount);

            if (Data == null)
            {
                return null;
            }

            HydroColorProcessedMeasurement DataRecord = new();

            DataRecord.WaterImageData.LocalTimeStamp = timestamp;
            DataRecord.WaterImageData.UTCOffset = ParseNumber(Data[2]);

            DataRecord.MeasurementName = Data[3].Replace('_', ' ');
            DataRecord.HydroColorVersion = Data[4];
            DataRecord.WaterImageData.ImageLocation.Latitude = ParseNumber(Data[5]);
            DataRecord.WaterImageData.ImageLocation.Longitude = ParseNumber(Data[6]);
            DataRecord.WaterImageData.ImageLocation.Accuracy = ParseNumber(Data[7], 0); // older records can have an empty accuracy column

            DataRecord.MeasurementProducts.Reflectance.Red = ParseNumber(Data[8]);
            DataRecord.MeasurementProducts.Reflectance.Green = ParseNumber(Data[9]);
            DataRecord.MeasurementProducts.Reflectance.Blue = ParseNumber(Data[10]);
            DataRecord.MeasurementProducts.WaterTurbidity = ParseNumber(Data[11]);
            DataRecord.MeasurementProducts.SPM = ParseNumber(Data[12]);
            DataRecord.MeasurementProducts.Backscatter_red = ParseNumber(Data[13]);

            DataRecord.WaterImageData.SunElevationAngle = ParseNumber(Data[14]);
            DataRecord.WaterImageData.SunAzimuthAngle = ParseNumber(Data[15]);
            DataRecord.WaterImageData.MagneticDeclination = ParseNumber(Data[16]);

            DataRecord.DeviceManufacturer = Data[17];
            DataRecord.DeviceModel = Data[18];
            DataRecord.DeviceOSVersion = Data[19];
            DataRecord.WaterImageData.BayerFilterPattern = ParseBayerPattern(Data[20]);

            DataRecord.GrayCardImageData.ImageCapturedAtCorrectAngles = ParseBool(Data[21]);
            DataRecord.GrayCardImageData.ImageAzimuthAngle = ParseNumber(Data[22]);
            DataRecord.GrayCardImageData.ImageOffNadirAngle = ParseNumber(Data[23]);
            DataRecord.GrayCardImageData.ExposureTime = ParseNumber(Data[24]);
            DataRecord.GrayCardImageData.SensorSensitivity = ParseNumber(Data[25]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Red = ParseNumber(Data[26]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Green = ParseNumber(Data[27]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Blue = ParseNumber(Data[28]);

            DataRecord.SkyImageData.ImageCapturedAtCorrectAngles = ParseBool(Data[29]);
            DataRecord.SkyImageData.ImageAzimuthAngle = ParseNumber(Data[30]);
            DataRecord.SkyImageData.ImageOffNadirAngle = ParseNumber(Data[31]);
            DataRecord.SkyImageData.ExposureTime = ParseNumber(Data[32]);
            DataRecord.SkyImageData.SensorSensitivity = ParseNumber(Data[33]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Red = ParseNumber(Data[34]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Green = ParseNumber(Data[35]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Blue = ParseNumber(Data[36]);

            DataRecord.WaterImageData.ImageCapturedAtCorrectAngles = ParseBool(Data[37]);
            DataRecord.WaterImageData.ImageAzimuthAngle = ParseNumber(Data[38]);
            DataRecord.WaterImageData.ImageOffNadirAngle = ParseNumber(Data[39]);
            DataRecord.WaterImageData.ExposureTime = ParseNumber(Data[40]);
            DataRecord.WaterImageData.SensorSensitivity = ParseNumber(Data[41]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Red = ParseNumber(Data[42]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Green = ParseNumber(Data[43]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Blue = ParseNumber(Data[44]);
            try
            {
                DataRecord.GrayCardImageData.JpegImage = File.ReadAllBytes(GetImagePath(timestamp, "GrayCard"));
                DataRecord.SkyImageData.JpegImage = File.ReadAllBytes(GetImagePath(timestamp, "Sky"));
                DataRecord.WaterImageData.JpegImage = File.ReadAllBytes(GetImagePath(timestamp, "Water"));
            }
            catch
            { }

            return DataRecord;
            }

        public HydroColorProcessedMeasurement GetDataRecord_v2p0(DateTime timestamp)
        {
            // The version 2.0 format has one column fewer; the exact count is checked below
            string[] Data = FindRecord(timestamp, 2);

            if (Data == null)
            {
                return null;
            }

            if (Data.Length != 44)
            {
                throw new FormatException();
            }

            HydroColorProcessedMeasurement DataRecord = new();

            DataRecord.WaterImageData.LocalTimeStamp = timestamp;
            DataRecord.WaterImageData.UTCOffset = ParseNumber(Data[2]);

            DataRecord.MeasurementName = Data[3].Replace('_',' ');
            DataRecord.WaterImageData.ImageLocation.Latitude = ParseNumber(Data[4]);
            DataRecord.WaterImageData.ImageLocation.Longitude = ParseNumber(Data[5]);
            DataRecord.WaterImageData.ImageLocation.Accuracy = ParseNumber(Data[6], 0);

            DataRecord.MeasurementProducts.Reflectance.Red = ParseNumber(Data[7]);
            DataRecord.MeasurementProducts.Reflectance.Green = ParseNumber(Data[8]);
            DataRecord.MeasurementProducts.Reflectance.Blue = ParseNumber(Data[9]);
            DataRecord.MeasurementProducts.WaterTurbidity = ParseNumber(Data[10]);
            DataRecord.MeasurementProducts.SPM = ParseNumber(Data[11]);
            DataRecord.MeasurementProducts.Backscatter_red = ParseNumber(Data[12]);

            DataRecord.WaterImageData.SunElevationAngle = ParseNumber(Data[13]);
            DataRecord.WaterImageData.SunAzimuthAngle = ParseNumber(Data[14]);
            DataRecord.WaterImageData.MagneticDeclination = ParseNumber(Data[15]);

            DataRecord.DeviceManufacturer = Data[16];
            DataRecord.DeviceModel = Data[17];
            DataRecord.DeviceOSVersion = Data[18];
            DataRecord.WaterImageData.BayerFilterPattern = ParseBayerPattern(Data[19]);

            DataRecord.GrayCardImageData.ImageCapturedAtCorrectAngles = ParseBool(Data[20]);
            DataRecord.GrayCardImageData.ImageAzimuthAngle = ParseNumber(Data[21]);
            DataRecord.GrayCardImageData.ImageOffNadirAngle = ParseNumber(Data[22]);
            DataRecord.GrayCardImageData.ExposureTime = ParseNumber(Data[23]);
            DataRecord.GrayCardImageData.SensorSensitivity = ParseNumber(Data[24]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Red = ParseNumber(Data[25]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Green = ParseNumber(Data[26]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Blue = ParseNumber(Data[27]);

            DataRecord.SkyImageData.ImageCapturedAtCorrectAngles = ParseBool(Data[28]);
            DataRecord.SkyImageData.ImageAzimuthAngle = ParseNumber(Data[29]);
            DataRecord.SkyImageData.ImageOffNadirAngle = ParseNumber(Data[30]);
            DataRecord.SkyImageData.ExposureTime = ParseNumber(Data[31]);
            DataRecord.SkyImageData.SensorSensitivity = ParseNumber(Data[32]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Red = ParseNumber(Data[33]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Green = ParseNumber(Data[34]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Blue = ParseNumber(Data[35]);

            DataRecord.WaterImageData.ImageCapturedAtCorrectAngles = ParseBool(Data[36]);
            DataRecord.WaterImageData.ImageAzimuthAngle = ParseNumber(Data[37]);
            DataRecord.WaterImageData.ImageOffNadirAngle = ParseNumber(Data[38]);
            DataRecord.WaterImageData.ExposureTime = ParseNumber(Data[39]);
            DataRecord.WaterImageData.SensorSensitivity = ParseNumber(Data[40]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Red = ParseNumber(Data[41]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Green = ParseNumber(Data[42]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Blue = ParseNumber(Data[43]);

            // A missing thumbnail image must not stop the record from being read
            try
            {
                DataRecord.GrayCardImageData.JpegImage = File.ReadAllBytes(GetImagePath(timestamp, "GrayCard"));
                DataRecord.SkyImageData.JpegImage = File.ReadAllBytes(GetImagePath(timestamp, "Sky"));
                DataRecord.WaterImageData.JpegImage = File.ReadAllBytes(GetImagePath(timestamp, "Water"));
            }
            catch
            { }

            return DataRecord;
        }

        public void DeleteDataRecord(DateTime timestamp)
        {
            string DataFilePath = GetDataFileName();

            // Keep the header and every line that is not the record being deleted,
            // including any lines that cannot be read.
            List<string> linesToKeep = new();
            foreach (string line in File.ReadLines(DataFilePath))
            {
                string[] data = line.Split(' ');
                bool isRecordToDelete = data.Length > 1
                    && TryParseTimestamp(data[0], data[1], out DateTime lineTimestamp)
                    && lineTimestamp == timestamp;

                if (!isRecordToDelete)
                {
                    linesToKeep.Add(line);
                }
            }

            // Write the new file completely, then swap it in as a single step,
            // so an interruption cannot leave the app without a data file.
            string TempFilePath = Path.Combine(DataFolderPath, "HydroColor_Temp_Datafile.txt");
            File.WriteAllLines(TempFilePath, linesToKeep);
            File.Move(TempFilePath, DataFilePath, overwrite: true);

            File.Delete(GetImagePath(timestamp, "GrayCard"));
            File.Delete(GetImagePath(timestamp, "Sky"));
            File.Delete(GetImagePath(timestamp, "Water"));
        }

        public void WriteBinaryArray2File(string FileName, UInt16[,] Data)
        {
            string DataFilePath = Path.Combine(DataFolderPath, FileName);

            FileStream fs = new FileStream(DataFilePath, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            for (int i = 0; i < Data.GetLength(1); i++)
            {
                for (int j = 0; j < Data.GetLength(0); j++)
                {
                    bw.Write(Data[j, i]);
                }
            }
            bw.Close();
            fs.Close();
        }

        public List<string> GetSavedEmailAddress()
        {
            if (Preferences.Default.ContainsKey(PreferenceKeys.SavedEmailAddresses))
            {
                string addresses = Preferences.Default.Get(PreferenceKeys.SavedEmailAddresses, string.Empty);

                if (string.IsNullOrEmpty(addresses))
                {
                    return null;
                }
                else
                {
                    return addresses.Split(' ').ToList();
                }
            }
            else
            {
                return null;
            }
        }

        public void SaveNewEmailAddress(string newAddress)
        {
            // addresses are stored in a space delimited string
            // ensure there are no spaces in the entered email address
            // email address should never have spaces, so it will be invalid anyway
            newAddress = newAddress.Replace(" ", "_");

            List<string> storedAddresses = GetSavedEmailAddress();

            string newStoredAddresses;
            if (storedAddresses == null)
            {
                newStoredAddresses = newAddress;
            }
            else
            {
                newStoredAddresses = string.Join(' ', storedAddresses) + " " + newAddress;
            }
            Preferences.Default.Set(PreferenceKeys.SavedEmailAddresses, newStoredAddresses);
        }

        public void RemoveEmailAddress(string address)
        {
            List<string> storedAddresses = GetSavedEmailAddress();
            storedAddresses.Remove(address);
            Preferences.Default.Set(PreferenceKeys.SavedEmailAddresses, string.Join(' ', storedAddresses));
        }

        private async Task<string> DisplayEmailAddresses()
        {
            List<string> storedAddresses = GetSavedEmailAddress();
            if (storedAddresses == null)
            {
                storedAddresses = new List<string>();
            }
            else if (storedAddresses.Count > 0)
            {
                storedAddresses.Add(Strings.FileReaderWriter_DeleteEmail);
            }
            storedAddresses.Insert(0, Strings.FileReaderWriter_AddEmail);

            return await ChooseOption(Strings.FileReaderWriter_SelectEmailMessage, Strings.FileReaderWriter_SelectEmailCancelButton, storedAddresses);
        }

        // Shows a chooser and returns the option that was picked.
        // Returns null if Cancel was pressed or the chooser was dismissed another way
        // (on Android, tapping outside the chooser returns nothing at all).
        // The choice is recognised by being one of the options offered, so it does not
        // depend on the text of the Cancel button.
        private static async Task<string> ChooseOption(string title, string cancelButtonText, List<string> options)
        {
            string selection = await Shell.Current.CurrentPage.DisplayActionSheetAsync(title, cancelButtonText, null, options.ToArray());
            return selection != null && options.Contains(selection) ? selection : null;
        }

        // Asks for a new email address until one containing '@' is entered.
        // Returns null if the prompt is cancelled or left empty.
        private static async Task<string> PromptForNewEmailAddress()
        {
            string entered = "";
            while (true)
            {
                entered = await Shell.Current.CurrentPage.DisplayPromptAsync(Strings.FileReaderWriter_AddEmailTitle, Strings.FileReaderWriter_AddEmailMessage, keyboard: Keyboard.Email, initialValue: entered);
                if (string.IsNullOrWhiteSpace(entered)) // cancel pressed
                {
                    return null;
                }

                entered = entered.Trim();
                if (entered.Contains('@'))
                {
                    return entered;
                }

                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.FileReaderWriter_InvalidEmailTitle, Strings.FileReaderWriter_InvalidEmailMessage, Strings.FileReaderWriter_InvalidEmailDismissButton);
            }
        }

        public async void EmailDataFile()
        {
            string address = await DisplayEmailAddresses();
            if (address == null) // cancelled
            {
                return;
            }

            while (address == Strings.FileReaderWriter_DeleteEmail || address == Strings.FileReaderWriter_AddEmail)
            {
                if (address == Strings.FileReaderWriter_AddEmail)
                {
                    string newEmailAddress = await PromptForNewEmailAddress();
                    if (newEmailAddress == null) // cancel pressed
                    {
                        return;
                    }

                    SaveNewEmailAddress(newEmailAddress);
                    address = await DisplayEmailAddresses();
                    if (address == null) // cancelled
                    {
                        return;
                    }
                }
                else
                {
                    List<string> storedAddresses = GetSavedEmailAddress();
                    address = await ChooseOption(Strings.FileReaderWriter_DeleteEmailMessage, Strings.FileReaderWriter_DeleteEmailCancelButton, storedAddresses);
                    if (address == null) // cancelled
                    {
                        return;
                    }
                    bool confirmDelete = await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.FileReaderWriter_ConfirmDeleteEmailTitle, $"{Strings.FileReaderWriter_ConfirmDeleteEmailMessage}\n\n{address}", Strings.FileReaderWriter_ConfirmDeleteEmailDeleteButton, Strings.FileReaderWriter_ConfirmDeleteEmailCancelButton);
                    if (!confirmDelete)
                    {
                        return;
                    }

                    RemoveEmailAddress(address);
                    address = await DisplayEmailAddresses();
                    if (address == null) // cancelled
                    {
                        return;
                    }

                }
            }

            string savedName = Preferences.Default.Get(PreferenceKeys.UserEnteredDataFileSuffix, "");
            string customFilenameSuffix = await Shell.Current.CurrentPage.DisplayPromptAsync(Strings.FileReaderWriter_ConfirmSendEmailTitle, $"{Strings.FileReaderWriter_ConfirmSendEmailMessage_1}\n\n{address}\n\n {Strings.FileReaderWriter_ConfirmSendEmailMessage_2}", Strings.FileReaderWriter_ConfirmSendEmailMessageSendButton, Strings.FileReaderWriter_ConfirmSendEmailMessageCancelButton, initialValue: savedName);
            if (customFilenameSuffix == null)
            {
                return;
            }

            Preferences.Default.Set(PreferenceKeys.UserEnteredDataFileSuffix, customFilenameSuffix);

            string attachmentName;
            if (string.IsNullOrEmpty(customFilenameSuffix))
            {
                attachmentName = DataFileName;
            }
            else
            {
                attachmentName = $"HydroColor_{customFilenameSuffix}.txt";
            }


            var EmailBusyPopup = new SendingEmailPopup();
            EmailBusyPopup.BindingContext = this;
            bool EmailSent = false;
            string emailErrorDetail = "";
            // ShowPopupAsync only completes when the popup is closed, so it must not be awaited here.
            // Keep the task and await it after the popup is closed below.
            var popupTask = Shell.Current.CurrentPage.ShowPopupAsync(EmailBusyPopup);
            try
            {
                // Note: MailKit has failed on older Android devices with an ssl handshake exception.
                await MailKitSendEmail(address, attachmentName);
                EmailSent = true;
            }
            catch (Exception ex)
            {
                // EmailSent stays false and the "Email Failed" message is shown below, with the reason.
                emailErrorDetail = ex.Message;
            }

            await EmailBusyPopup.CloseAsync();
            await popupTask;

            if (EmailSent)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.FileReaderWriter_EmailSentTitle, $"{Strings.FileReaderWriter_EmailSentMessage_1}\n\n{address}\n\n{Strings.FileReaderWriter_EmailSentMessage_2}", Strings.FileReaderWriter_EmailSentDismissButton);
            }
            else
            {
                string emailFailedMessage;
#if ANDROID
                    emailFailedMessage = Strings.FileReaderWriter_EmailFailedMessageAndroid;
#elif IOS
                emailFailedMessage = Strings.FileReaderWriter_EmailFailedMessageIOS;
#endif
                if (!string.IsNullOrEmpty(emailErrorDetail))
                {
                    emailFailedMessage += "\n\nError: " + emailErrorDetail;
                }
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.FileReaderWriter_EmailFailedTitle, emailFailedMessage, Strings.FileReaderWriter_EmailFailedDismissButton);
            }

        }

        public async Task MailKitSendEmail(string address, string attachmentName)
        {
            await Task.Factory.StartNew(() => { 
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(EmailSenderName, Secrets.EmailAddress));
            message.To.Add(new MailboxAddress(EmailReceiverName, address));
            message.Subject = EmailSubject;

            var builder = new BodyBuilder();
            builder.TextBody = EmailBody;
            string DataFilePath = GetDataFileName();
            byte[] byteData = File.ReadAllBytes(DataFilePath);
            builder.Attachments.Add(attachmentName, byteData);
            message.Body = builder.ToMessageBody();

            SmtpClient client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            client.Connect(Secrets.EmailHost, 587, SecureSocketOptions.Auto);
            client.Authenticate(Secrets.EmailUserName, Secrets.EmailPassword);
            client.Send(message);
            client.Disconnect(true);
            });
        }

    }
}
