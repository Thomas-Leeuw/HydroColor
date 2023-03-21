using HydroColor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydroColor.Services
{
    public static class VersionCorrections
    {

        // The HydroColor version number was added to the data file in version 2.1
        // Read in the existing measurements and rewrite them with the version number
        public static void UpdateTo2p1DataFileFormatIfNeeded()
        {

            try
            {
                bool datafileUpdated = Preferences.Default.Get(PreferenceKeys.DatafileUpdatedToV2p1, false);

                if (!datafileUpdated)
                {
                    FileReaderWriter fileReaderWriter = new FileReaderWriter();

                    if (fileReaderWriter.DataFileExists())
                    {

                        List<DataLibraryItem> DataItems = fileReaderWriter.LoadDataSummary();

                        if (DataItems.Count == 0)
                        {
                            fileReaderWriter.DeleteDataFile();
                        }
                        else
                        {
                            string tempFileName = "HydroColor_DataFile_temp.txt";
                            string tempFileFullPath = Path.Combine(fileReaderWriter.DataFolderPath, tempFileName);
                            if (File.Exists(tempFileFullPath))
                            {
                                File.Delete(tempFileFullPath);
                            }

                            foreach (DataLibraryItem dataRecord in DataItems)
                            {
                                HydroColorProcessedMeasurement ProcMeas = new HydroColorProcessedMeasurement();
                                try
                                {
                                    ProcMeas = fileReaderWriter.GetDataRecord_v2p0(dataRecord.LocalTimestamp);
                                }
                                catch (FormatException)
                                {
                                    Preferences.Default.Set(PreferenceKeys.DatafileUpdatedToV2p1, true);
                                    return;
                                }
                                ProcMeas.HydroColorVersion = "2.0";
                                fileReaderWriter.WriteTextDataRecord(ProcMeas, tempFileName);
                            }
                            fileReaderWriter.DeleteDataFile();
                            File.Move(tempFileFullPath, fileReaderWriter.GetDataFileName());
                        }
                    }

                    Preferences.Default.Set(PreferenceKeys.DatafileUpdatedToV2p1, true);
                }
            }
            catch
            {
                FileReaderWriter fileReaderWriter = new FileReaderWriter();
                fileReaderWriter.DeleteDataFile();
                Preferences.Default.Set(PreferenceKeys.DatafileUpdatedToV2p1, true);
            }
        }

    }
}
