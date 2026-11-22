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
        const string TempFileName = "HydroColor_DataFile_temp.txt";

        // The HydroColor version number was added to the data file in version 2.1
        // Read in the existing measurements and rewrite them with the version number
        //
        // The original data file is only replaced after every record has been converted.
        // If anything goes wrong it is left exactly as it was; it is never deleted because the update failed.
        public static void UpdateTo2p1DataFileFormatIfNeeded()
        {
            if (Preferences.Default.Get(PreferenceKeys.DatafileUpdatedToV2p1, false))
            {
                return;
            }

            string tempFileFullPath = null;

            try
            {
                FileReaderWriter fileReaderWriter = new FileReaderWriter();
                tempFileFullPath = Path.Combine(fileReaderWriter.DataFolderPath, TempFileName);

                if (fileReaderWriter.DataFileExists())
                {
                    List<DataLibraryItem> DataItems = fileReaderWriter.LoadDataSummary(out int unreadableCount);

                    if (DataItems.Count == 0 && unreadableCount == 0)
                    {
                        // Only a header, or empty: there is nothing to lose. Remove it so the
                        // next measurement starts a new file with the current header.
                        fileReaderWriter.DeleteDataFile();
                    }
                    else if (unreadableCount == 0 && ConvertAllRecords(fileReaderWriter, DataItems, tempFileFullPath))
                    {
                        // Every record converted: swap the new file in as a single step.
                        File.Move(tempFileFullPath, fileReaderWriter.GetDataFileName(), overwrite: true);
                    }
                    // Otherwise (a line could not be read, or the file is already in the current
                    // format) the original data file is left untouched.
                }
            }
            catch
            {
                // Leave the data file as it is.
            }
            finally
            {
                try
                {
                    if (tempFileFullPath != null && File.Exists(tempFileFullPath))
                    {
                        File.Delete(tempFileFullPath);
                    }
                }
                catch
                { }

                // One attempt only
                Preferences.Default.Set(PreferenceKeys.DatafileUpdatedToV2p1, true);
            }
        }

        // Writes every record to the temporary file in the current format.
        // Returns false if any record is not in the version 2.0 format or cannot be read.
        static bool ConvertAllRecords(FileReaderWriter fileReaderWriter, List<DataLibraryItem> DataItems, string tempFileFullPath)
        {
            if (File.Exists(tempFileFullPath))
            {
                File.Delete(tempFileFullPath);
            }

            foreach (DataLibraryItem dataRecord in DataItems)
            {
                HydroColorProcessedMeasurement ProcMeas;
                try
                {
                    ProcMeas = fileReaderWriter.GetDataRecord_v2p0(dataRecord.LocalTimestamp);
                }
                catch (FormatException)
                {
                    // Not a version 2.0 record: the file is already in the current format
                    return false;
                }

                if (ProcMeas == null)
                {
                    return false;
                }

                ProcMeas.HydroColorVersion = "2.0";
                fileReaderWriter.WriteTextDataRecord(ProcMeas, TempFileName);
            }

            return true;
        }

    }
}
