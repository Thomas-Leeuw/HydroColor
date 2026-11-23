using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
using HydroColor.Resources.Strings;
using HydroColor.Services;
using HydroColor.Views;
using System.Collections.ObjectModel;

namespace HydroColor.ViewModels
{
    public partial class LibraryViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<DataLibraryItem> dataLibraryItems;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EmailDataFileCommand))]
        bool measurementsExist;

        // Number of lines in the data file that could not be read (they are not shown in the list)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasUnreadableMeasurements))]
        [NotifyPropertyChangedFor(nameof(UnreadableMeasurementsText))]
        [NotifyCanExecuteChangedFor(nameof(EmailDataFileCommand))]
        int unreadableMeasurementCount;

        public bool HasUnreadableMeasurements => UnreadableMeasurementCount > 0;

        public string UnreadableMeasurementsText => string.Format(Strings.Library_UnreadableMeasurementsLink, UnreadableMeasurementCount);

        // The data file can be emailed whenever it has any records, including ones that cannot be read
        bool CanEmailDataFile => MeasurementsExist || HasUnreadableMeasurements;

        FileReaderWriter DataFile = new();

        [RelayCommand]
        void ViewAppearing()
        {
            RefreshLibraryItemsList();
        }

        void RefreshLibraryItemsList()
        {
            List<DataLibraryItem> DataItems = DataFile.LoadDataSummary(out int unreadableCount);

            UnreadableMeasurementCount = unreadableCount;
            MeasurementsExist = DataItems.Count > 0;

            // place newest measurements at the top of the list
            DataItems.Reverse();
            DataLibraryItems = new ObservableCollection<DataLibraryItem>(DataItems);
        }

        [RelayCommand]
        async Task ItemTapped(DataLibraryItem item)
        {
            HydroColorProcessedMeasurement ProcMeas = DataFile.GetDataRecord(item.LocalTimestamp);

            if (ProcMeas == null)
            {
                // The record is missing or incomplete in the data file
                await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.Library_MeasurementNotOpenedTitle, Strings.Library_MeasurementNotOpenedMessage, Strings.Library_MeasurementNotOpenedDismissButton);
                return;
            }

            await Shell.Current.GoToAsync(nameof(DataView), new Dictionary<string, object>
            {
                ["MeasurementData"] = ProcMeas
            });
        }

        [RelayCommand]
        async Task DeleteItem(DataLibraryItem ItemToDelete)
        {
            bool confirm = await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.Library_DeleteMeasurementTitle, $"{ItemToDelete.MeasurementName}\n{ItemToDelete.LocalTimestamp}", Strings.Library_DeleteMeasurementDeleteButton, Strings.Library_DeleteMeasurementCancelButton);
            if (!confirm)
            {
                return;
            }
            DataFile.DeleteDataRecord(ItemToDelete.LocalTimestamp);

            RefreshLibraryItemsList();
        }

        [RelayCommand(CanExecute = nameof(CanEmailDataFile))]
        void EmailDataFile()
        {
            DataFile.EmailDataFile();
        }

        [RelayCommand]
        async Task UnreadableMeasurements()
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.Library_UnreadableMeasurementsTitle, string.Format(Strings.Library_UnreadableMeasurementsMessage, UnreadableMeasurementCount), Strings.Library_UnreadableMeasurementsDismissButton);
        }

        [RelayCommand]
        async Task MissingMeasurements()
        {

            string messageBody;

#if ANDROID
                messageBody = Strings.Library_MissingMeasurmentsMessageAndroid;
#endif

#if IOS
            messageBody = Strings.Library_MissingMeasurmentsMessageIOS;
#endif

            await Shell.Current.CurrentPage.DisplayAlertAsync(Strings.Library_MissingMeasurmentsTitle, messageBody , Strings.Library_MissingMeasurmentsDismissButtion);

        }
    }
}
