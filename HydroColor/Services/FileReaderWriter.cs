using HydroColor.Models;
using MimeKit;
using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using SendGrid.Helpers.Mail;
using HydroColor.Resources.Strings;
using CommunityToolkit.Maui.Views;
using HydroColor.Views;

namespace HydroColor.Services
{
    public class FileReaderWriter
    {

        const string DataFileName = "HydroColor_Datafile.txt";

        public string DataFolderPath
        {
#if ANDROID
            get { return Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath; }      
#endif

#if IOS
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

        public void WriteDataRecord(HydroColorProcessedMeasurement ProcMeas)
        {

            // All thumbnail image file names use the water image timestamp
            string imageFileName = Path.Combine(DataFolderPath, $"{ProcMeas.WaterImageData.LocalTimeStamp.ToString("yyyyMMdd_HHmmss")}_GrayCard.jpg");
            File.WriteAllBytes(imageFileName, ProcMeas.GrayCardImageData.JpegImage);

            imageFileName = Path.Combine(DataFolderPath, $"{ProcMeas.WaterImageData.LocalTimeStamp.ToString("yyyyMMdd_HHmmss")}_Sky.jpg");
            File.WriteAllBytes(imageFileName, ProcMeas.SkyImageData.JpegImage);

            imageFileName = Path.Combine(DataFolderPath, $"{ProcMeas.WaterImageData.LocalTimeStamp.ToString("yyyyMMdd_HHmmss")}_Water.jpg");
            File.WriteAllBytes(imageFileName, ProcMeas.WaterImageData.JpegImage);

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
                { "Date",                        ProcMeas.WaterImageData.LocalTimeStamp.ToString("yyyy/MM/dd") },
                { "Time",                        ProcMeas.WaterImageData.LocalTimeStamp.ToString("HH:mm:ss") },
                { "UTC_Offset",                  ProcMeas.WaterImageData.UTCOffset.ToString("F1") },
                { "Name",                        measurementName },    // data file is space delimted, replace spacing in measurement name with underscore
                { "HydroColor_Version",          ProcMeas.HydroColorVersion },

                { "Latitude",                    ProcMeas.WaterImageData.ImageLocation.Latitude.ToString("F5") },
                { "Longitude",                   ProcMeas.WaterImageData.ImageLocation.Longitude.ToString("F5") },
                { "GPS_Accuracy_meters",         ProcMeas.WaterImageData.ImageLocation.Accuracy?.ToString("F1") },

                { "Reflectance_Red_1/sr",        ProcMeas.MeasurementProducts.Reflectance.Red.ToString("F4") },
                { "Reflectance_Green_1/sr",      ProcMeas.MeasurementProducts.Reflectance.Green.ToString("F4") },
                { "Reflectance_Blue_1/sr",       ProcMeas.MeasurementProducts.Reflectance.Blue.ToString("F4") },
                { "Turbidity_NTU",               ProcMeas.MeasurementProducts.WaterTurbidity.ToString("F0") },
                { "SPM_mg/L",                    ProcMeas.MeasurementProducts.SPM.ToString("F0")},
                { "Backscatter_Red_1/m",         ProcMeas.MeasurementProducts.Backscatter_red.ToString("F2")},

                { "Sun_Elevation_deg",           ProcMeas.WaterImageData.SunElevationAngle.ToString("F0") },
                { "Sun_Azimuth_deg",             ProcMeas.WaterImageData.SunAzimuthAngle.ToString("F0") },
                { "Magnetic_Declination_deg",    ProcMeas.WaterImageData.MagneticDeclination.ToString("F1") },

                { "Device_Manufacturer",         ProcMeas.DeviceManufacturer.Replace(" ", "_") },
                { "Device_Model",                ProcMeas.DeviceModel.Replace(" ", "_") },
                { "Device_OS_Version",           ProcMeas.DeviceOSVersion.Replace(" ", "_") },
                { "Device_Bayer_Pattern",        ProcMeas.WaterImageData.BayerFilterPattern.ToString() },

                { "GrayCard_Capture_Angles_Correct", ProcMeas.GrayCardImageData.ImageCapturedAtCorrectAngles.ToString() },
                { "GrayCard_Heading_deg",        ProcMeas.GrayCardImageData.ImageAzimuthAngle.ToString("F0") },
                { "GrayCard_Pitch_deg",          ProcMeas.GrayCardImageData.ImageOffNadirAngle.ToString("F0") },
                { "GrayCard_Exposure_sec",       ProcMeas.GrayCardImageData.ExposureTime.ToString("F6") },
                { "GrayCard_Sensor_Sensitivity", ProcMeas.GrayCardImageData.SensorSensitivity.ToString("F2") },
                { "GrayCard_Red_Light_Level",    ProcMeas.MeasurementProducts.GrayCardRelativeLightLevel.Red.ToString("F5") },
                { "GrayCard_Green_Light_Level",  ProcMeas.MeasurementProducts.GrayCardRelativeLightLevel.Green.ToString("F5") },
                { "GrayCard_Blue_Light_Level",   ProcMeas.MeasurementProducts.GrayCardRelativeLightLevel.Blue.ToString("F5") },

                { "Sky_Capture_Angles_Correct",  ProcMeas.SkyImageData.ImageCapturedAtCorrectAngles.ToString() },
                { "Sky_Heading_deg",             ProcMeas.SkyImageData.ImageAzimuthAngle.ToString("F0") },
                { "Sky_Pitch_deg",               ProcMeas.SkyImageData.ImageOffNadirAngle.ToString("F0") },
                { "Sky_Exposure_sec",            ProcMeas.SkyImageData.ExposureTime.ToString("F6") },
                { "Sky_Sensor_Sensitivity",      ProcMeas.SkyImageData.SensorSensitivity.ToString("F2") },
                { "Sky_Red_Light_Level",         ProcMeas.MeasurementProducts.SkyRelativeLightLevel.Red.ToString("F5") },
                { "Sky_Green_Light_Level",       ProcMeas.MeasurementProducts.SkyRelativeLightLevel.Green.ToString("F5") },
                { "Sky_Blue_Light_Level",        ProcMeas.MeasurementProducts.SkyRelativeLightLevel.Blue.ToString("F5") },

                { "Water_Capture_Angles_Correct",ProcMeas.WaterImageData.ImageCapturedAtCorrectAngles.ToString() },
                { "Water_Heading_deg",           ProcMeas.WaterImageData.ImageAzimuthAngle.ToString("F0") },
                { "Water_Pitch_deg",             ProcMeas.WaterImageData.ImageOffNadirAngle.ToString("F0") },
                { "Water_Exposure_sec",          ProcMeas.WaterImageData.ExposureTime.ToString("F6") },
                { "Water_Sensor_Sensitivity",    ProcMeas.WaterImageData.SensorSensitivity.ToString("F2") },
                { "Water_Red_Light_Level",       ProcMeas.MeasurementProducts.WaterRelativeLightLevel.Red.ToString("F5") },
                { "Water_Green_Light_Level",     ProcMeas.MeasurementProducts.WaterRelativeLightLevel.Green.ToString("F5") },
                { "Water_Blue_Light_Level",      ProcMeas.MeasurementProducts.WaterRelativeLightLevel.Blue.ToString("F5") }
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
            List<DataLibraryItem> LibraryList = new();

            string DataFilePath = GetDataFileName();

            if (!File.Exists(DataFilePath))
            {
                return LibraryList;
            }

            int lineNumber = 0;
            foreach (string line in System.IO.File.ReadLines(DataFilePath))
            {
                if (lineNumber == 0) // skip header line
                {
                    lineNumber++;
                    continue;
                }

                DataLibraryItem DataRecordItem = new();

                string[] dataRecord = line.Split(' ');

                DataRecordItem.LocalTimestamp = DateTime.ParseExact(dataRecord[0] + dataRecord[1], "yyyy/MM/ddHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                DataRecordItem.MeasurementName = dataRecord[3].Replace('_', ' ');
                DataRecordItem.WaterImageURI = new Uri(Path.Combine(DataFolderPath, $"{DataRecordItem.LocalTimestamp.ToString("yyyyMMdd_HHmmss")}_Water.jpg")).AbsolutePath;

                LibraryList.Add(DataRecordItem);

                lineNumber++;
            }

            return LibraryList;

        }


        public HydroColorProcessedMeasurement GetDataRecord(DateTime timestamp)
        {
            string DataFilePath = GetDataFileName();

            string[] DataRecordLine = File.ReadLines(DataFilePath).Where(line => line.Contains(timestamp.ToString("yyyy/MM/dd HH:mm:ss"))).ToArray();

            if (DataRecordLine.Count() == 0)
            {
                return null;
            }

            string[] Data = DataRecordLine[0].Split(' ');

            HydroColorProcessedMeasurement DataRecord = new();

            DataRecord.WaterImageData.LocalTimeStamp = DateTime.ParseExact($"{Data[0]}{Data[1]}", "yyyy/MM/ddHH:mm:ss", CultureInfo.InvariantCulture);
            DataRecord.WaterImageData.UTCOffset = double.Parse(Data[2]);

            DataRecord.MeasurementName = Data[3].Replace('_', ' ');
            DataRecord.HydroColorVersion = Data[4];
            DataRecord.WaterImageData.ImageLocation.Latitude = double.Parse(Data[5]);
            DataRecord.WaterImageData.ImageLocation.Longitude = double.Parse(Data[6]);
            DataRecord.WaterImageData.ImageLocation.Accuracy = double.Parse(Data[7]);

            DataRecord.MeasurementProducts.Reflectance.Red = double.Parse(Data[8]);
            DataRecord.MeasurementProducts.Reflectance.Green = double.Parse(Data[9]);
            DataRecord.MeasurementProducts.Reflectance.Blue = double.Parse(Data[10]);
            DataRecord.MeasurementProducts.WaterTurbidity = double.Parse(Data[11]);
            DataRecord.MeasurementProducts.SPM = double.Parse(Data[12]);
            DataRecord.MeasurementProducts.Backscatter_red = double.Parse(Data[13]);

            DataRecord.WaterImageData.SunElevationAngle = double.Parse(Data[14]);
            DataRecord.WaterImageData.SunAzimuthAngle = double.Parse(Data[15]);
            DataRecord.WaterImageData.MagneticDeclination = double.Parse(Data[16]);

            DataRecord.DeviceManufacturer = Data[17];
            DataRecord.DeviceModel = Data[18];
            DataRecord.DeviceOSVersion = Data[19];
            DataRecord.WaterImageData.BayerFilterPattern = (BayerFilterType)Enum.Parse(typeof(BayerFilterType), Data[20]);

            DataRecord.GrayCardImageData.ImageCapturedAtCorrectAngles = bool.Parse(Data[21]);
            DataRecord.GrayCardImageData.ImageAzimuthAngle = double.Parse(Data[22]);
            DataRecord.GrayCardImageData.ImageOffNadirAngle = double.Parse(Data[23]);
            DataRecord.GrayCardImageData.ExposureTime = double.Parse(Data[24]);
            DataRecord.GrayCardImageData.SensorSensitivity = double.Parse(Data[25]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Red = double.Parse(Data[26]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Green = double.Parse(Data[27]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Blue = double.Parse(Data[28]);

            DataRecord.SkyImageData.ImageCapturedAtCorrectAngles = bool.Parse(Data[29]);
            DataRecord.SkyImageData.ImageAzimuthAngle = double.Parse(Data[30]);
            DataRecord.SkyImageData.ImageOffNadirAngle = double.Parse(Data[31]);
            DataRecord.SkyImageData.ExposureTime = double.Parse(Data[32]);
            DataRecord.SkyImageData.SensorSensitivity = double.Parse(Data[33]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Red = double.Parse(Data[34]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Green = double.Parse(Data[35]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Blue = double.Parse(Data[36]);

            DataRecord.WaterImageData.ImageCapturedAtCorrectAngles = bool.Parse(Data[37]);
            DataRecord.WaterImageData.ImageAzimuthAngle = double.Parse(Data[38]);
            DataRecord.WaterImageData.ImageOffNadirAngle = double.Parse(Data[39]);
            DataRecord.WaterImageData.ExposureTime = double.Parse(Data[40]);
            DataRecord.WaterImageData.SensorSensitivity = double.Parse(Data[41]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Red = double.Parse(Data[42]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Green = double.Parse(Data[43]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Blue = double.Parse(Data[44]);

            DataRecord.GrayCardImageData.JpegImage = File.ReadAllBytes(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_GrayCard.jpg"));
            DataRecord.SkyImageData.JpegImage = File.ReadAllBytes(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_Sky.jpg"));
            DataRecord.WaterImageData.JpegImage = File.ReadAllBytes(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_Water.jpg"));

            return DataRecord;
        }

        public HydroColorProcessedMeasurement GetDataRecord_v2p0(DateTime timestamp)
        {
            string DataFilePath = GetDataFileName();

            string[] DataRecordLine = File.ReadLines(DataFilePath).Where(line => line.Contains(timestamp.ToString("yyyy/MM/dd HH:mm:ss"))).ToArray();

            if (DataRecordLine.Count() == 0)
            {
                return null;
            }

            string[] Data = DataRecordLine[0].Split(' ');

            if (Data.Length != 44)
            {
                throw new FormatException();
            }

            HydroColorProcessedMeasurement DataRecord = new();

            DataRecord.WaterImageData.LocalTimeStamp = DateTime.ParseExact($"{Data[0]}{Data[1]}", "yyyy/MM/ddHH:mm:ss", CultureInfo.InvariantCulture);
            DataRecord.WaterImageData.UTCOffset = double.Parse(Data[2]);

            DataRecord.MeasurementName = Data[3].Replace('_',' ');
            DataRecord.WaterImageData.ImageLocation.Latitude = double.Parse(Data[4]);
            DataRecord.WaterImageData.ImageLocation.Longitude = double.Parse(Data[5]);
            DataRecord.WaterImageData.ImageLocation.Accuracy = double.Parse(Data[6]);

            DataRecord.MeasurementProducts.Reflectance.Red = double.Parse(Data[7]);
            DataRecord.MeasurementProducts.Reflectance.Green = double.Parse(Data[8]);
            DataRecord.MeasurementProducts.Reflectance.Blue = double.Parse(Data[9]);
            DataRecord.MeasurementProducts.WaterTurbidity = double.Parse(Data[10]);
            DataRecord.MeasurementProducts.SPM = double.Parse(Data[11]);
            DataRecord.MeasurementProducts.Backscatter_red = double.Parse(Data[12]);

            DataRecord.WaterImageData.SunElevationAngle = double.Parse(Data[13]);
            DataRecord.WaterImageData.SunAzimuthAngle = double.Parse(Data[14]);
            DataRecord.WaterImageData.MagneticDeclination = double.Parse(Data[15]);

            DataRecord.DeviceManufacturer = Data[16];
            DataRecord.DeviceModel = Data[17];
            DataRecord.DeviceOSVersion = Data[18];
            DataRecord.WaterImageData.BayerFilterPattern = (BayerFilterType)Enum.Parse(typeof(BayerFilterType), Data[19]);

            DataRecord.GrayCardImageData.ImageCapturedAtCorrectAngles = bool.Parse(Data[20]);
            DataRecord.GrayCardImageData.ImageAzimuthAngle = double.Parse(Data[21]);
            DataRecord.GrayCardImageData.ImageOffNadirAngle = double.Parse(Data[22]);
            DataRecord.GrayCardImageData.ExposureTime = double.Parse(Data[23]);
            DataRecord.GrayCardImageData.SensorSensitivity = double.Parse(Data[24]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Red = double.Parse(Data[25]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Green = double.Parse(Data[26]);
            DataRecord.MeasurementProducts.GrayCardRelativeLightLevel.Blue = double.Parse(Data[27]);

            DataRecord.SkyImageData.ImageCapturedAtCorrectAngles = bool.Parse(Data[28]);
            DataRecord.SkyImageData.ImageAzimuthAngle = double.Parse(Data[29]);
            DataRecord.SkyImageData.ImageOffNadirAngle = double.Parse(Data[30]);
            DataRecord.SkyImageData.ExposureTime = double.Parse(Data[31]);
            DataRecord.SkyImageData.SensorSensitivity = double.Parse(Data[32]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Red = double.Parse(Data[33]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Green = double.Parse(Data[34]);
            DataRecord.MeasurementProducts.SkyRelativeLightLevel.Blue = double.Parse(Data[35]);

            DataRecord.WaterImageData.ImageCapturedAtCorrectAngles = bool.Parse(Data[36]);
            DataRecord.WaterImageData.ImageAzimuthAngle = double.Parse(Data[37]);
            DataRecord.WaterImageData.ImageOffNadirAngle = double.Parse(Data[38]);
            DataRecord.WaterImageData.ExposureTime = double.Parse(Data[39]);
            DataRecord.WaterImageData.SensorSensitivity = double.Parse(Data[40]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Red = double.Parse(Data[41]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Green = double.Parse(Data[42]);
            DataRecord.MeasurementProducts.WaterRelativeLightLevel.Blue = double.Parse(Data[43]);

            DataRecord.GrayCardImageData.JpegImage = File.ReadAllBytes(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_GrayCard.jpg"));
            DataRecord.SkyImageData.JpegImage = File.ReadAllBytes(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_Sky.jpg"));
            DataRecord.WaterImageData.JpegImage = File.ReadAllBytes(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_Water.jpg"));

            return DataRecord;
        }

        public void DeleteDataRecord(DateTime timestamp)
        {
            string DataFilePath = GetDataFileName();
            var linesToKeep = File.ReadLines(DataFilePath).Where(line => !line.Contains(timestamp.ToString("yyyy/MM/dd HH:mm:ss")));
            string TempFilePath = Path.Combine(DataFolderPath, "HydroColor_Temp_Datafile.txt");
            File.WriteAllLines(TempFilePath, linesToKeep);
            DeleteDataFile();
            File.Move(TempFilePath, DataFilePath);
            File.Delete(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_GrayCard.jpg"));
            File.Delete(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_Sky.jpg"));
            File.Delete(Path.Combine(DataFolderPath, $"{timestamp.ToString("yyyyMMdd_HHmmss")}_Water.jpg"));
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
            string address = await Shell.Current.CurrentPage.DisplayActionSheet(Strings.FileReaderWriter_SelectEmailMessage, Strings.FileReaderWriter_SelectEmailCancelButton, null, storedAddresses.ToArray());

            return address;
        }

        public async void EmailDataFile()
        {
            string address = await DisplayEmailAddresses();
            if (address.Contains(Strings.FileReaderWriter_SelectEmailCancelButton))
            {
                return;
            }

            while (address.Contains(Strings.FileReaderWriter_DeleteEmail) || address.Contains(Strings.FileReaderWriter_AddEmail))
            {
                if (address.Contains(Strings.FileReaderWriter_AddEmail))
                {
                    string newEmailAddress = await Shell.Current.CurrentPage.DisplayPromptAsync(Strings.FileReaderWriter_AddEmailTitle, Strings.FileReaderWriter_AddEmailMessage, keyboard: Keyboard.Email);
                    if (string.IsNullOrEmpty(newEmailAddress)) // cancel pressed
                    {
                        return;
                    }

                    SaveNewEmailAddress(newEmailAddress);
                    address = await DisplayEmailAddresses();
                    if (address.Contains(Strings.FileReaderWriter_SelectEmailCancelButton))
                    {
                        return;
                    }
                }
                else if (address.Contains(Strings.FileReaderWriter_DeleteEmail))
                {
                    List<string> storedAddresses = GetSavedEmailAddress();
                    address = await Shell.Current.CurrentPage.DisplayActionSheet(Strings.FileReaderWriter_DeleteEmailMessage, Strings.FileReaderWriter_DeleteEmailCancelButton, null, storedAddresses.ToArray());
                    if (address.Contains(Strings.FileReaderWriter_SelectEmailCancelButton))
                    {
                        return;
                    }
                    bool confirmDelete = await Shell.Current.CurrentPage.DisplayAlert(Strings.FileReaderWriter_ConfirmDeleteEmailTitle, $"{Strings.FileReaderWriter_ConfirmDeleteEmailMessage}\n\n{address}", Strings.FileReaderWriter_ConfirmDeleteEmailDeleteButton, Strings.FileReaderWriter_ConfirmDeleteEmailCancelButton);
                    if (!confirmDelete)
                    {
                        return;
                    }

                    RemoveEmailAddress(address);
                    address = await DisplayEmailAddresses();
                    if (address.Contains(Strings.FileReaderWriter_SelectEmailCancelButton))
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
            Shell.Current.CurrentPage.ShowPopup(EmailBusyPopup);
            try
            {
                // MailKit is not working on older Android devices, ssl handshake exeception always occurs
                // try MailKit first, then fall back on SendGrid Mail service
                await MailKitSendEmail(address, attachmentName);
                EmailSent = true;
            }
            catch
            {
                try
                {
                    await SendGridSendEmail(address, attachmentName);
                    EmailSent = true;
                }
                catch
                {

                }
            }

            EmailBusyPopup.Close();

            if (EmailSent)
            { 
                await Shell.Current.CurrentPage.DisplayAlert(Strings.FileReaderWriter_EmailSentTitle, $"{Strings.FileReaderWriter_EmailSentMessage_1}\n\n{address}\n\n{Strings.FileReaderWriter_EmailSentMessage_2}", Strings.FileReaderWriter_EmailSentDismissButton);
            }
            else
            {
                string emailFailedMessage;
#if ANDROID
                emailFailedMessage = Strings.FileReaderWriter_EmailFailedMessageAndroid;
#endif

#if IOS
                emailFailedMessage = Strings.FileReaderWriter_EmailFailedMessageIOS;
#endif
                await Shell.Current.CurrentPage.DisplayAlert(Strings.FileReaderWriter_EmailFailedTitle, emailFailedMessage, Strings.FileReaderWriter_EmailFailedDismissButton);
            }

        }

        public async Task MailKitSendEmail(string address, string attachmentName)
        {
            await Task.Factory.StartNew(() => { 
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(EmailSenderName, GmailCredentials.Email));
            message.To.Add(new MailboxAddress(EmailReceiverName, address));
            message.Subject = EmailSubject;

            var builder = new BodyBuilder();
            builder.TextBody = EmailBody + "\n\n(Sent With MailKit)";
            string DataFilePath = GetDataFileName();
            byte[] byteData = File.ReadAllBytes(DataFilePath);
            builder.Attachments.Add(attachmentName, byteData);
            message.Body = builder.ToMessageBody();

            SmtpClient client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            client.Connect(GmailCredentials.Host, 587, SecureSocketOptions.Auto);
            client.Authenticate(GmailCredentials.UserName, GmailCredentials.Password);
            client.Send(message);
            client.Disconnect(true);
            });
        }

        public async Task SendGridSendEmail(string address, string attachmentName)
        {
            await Task.Factory.StartNew(() =>
            {
                var sendGridClient = new SendGrid.SendGridClient(GmailCredentials.SendGridAPIKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress(GmailCredentials.Email, EmailSenderName),
                    Subject = EmailSubject,
                    PlainTextContent = EmailBody + "\n\n(Sent With SendGrid)"
                };
                msg.AddTo(new EmailAddress(address, EmailReceiverName));
                string DataFilePath = GetDataFileName();
                byte[] byteData = File.ReadAllBytes(DataFilePath);
                msg.AddAttachment(attachmentName, Convert.ToBase64String(byteData));
                var response = sendGridClient.SendEmailAsync(msg).Result;
                if (response.IsSuccessStatusCode == false)
                {
                    throw new Exception("SendGrid Email Failed");
                }
            });
        }

    }
}
