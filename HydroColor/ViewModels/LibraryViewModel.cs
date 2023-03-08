using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HydroColor.Models;
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
        bool showDeleteMeasurementsButtons;

        [ObservableProperty]
        string editListButtonText = "Edit List";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EmailDataFileCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditListCommand))]
        bool measurementsExist;

        FileReaderWriter DataFile = new();

        [RelayCommand]
        void ViewAppearing()
        {
            EditListButtonText = "Edit List";
            ShowDeleteMeasurementsButtons = false;
            RefreshLibraryItemsList();
        }

        void RefreshLibraryItemsList()
        {
            List<DataLibraryItem> DataItems = DataFile.LoadDataSummary();

            MeasurementsExist = DataItems.Count > 0;
            if (!MeasurementsExist)
            {
                EditListButtonText = "Edit List";
                ShowDeleteMeasurementsButtons = false;
            }

            // place newest measurements at the top of the list
            DataItems.Reverse();
            DataLibraryItems = new ObservableCollection<DataLibraryItem>(DataItems);
        }

        [RelayCommand]
        async Task ItemTapped(ItemTappedEventArgs e)
        {
            if (!ShowDeleteMeasurementsButtons)
            {
                HydroColorProcessedMeasurement ProcMeas = DataFile.GetDataRecord(((DataLibraryItem)e.Item).LocalTimestamp);
                await Shell.Current.GoToAsync(nameof(DataView), new Dictionary<string, object>
                {
                    ["MeasurementData"] = ProcMeas
                });
            }
        }

        [RelayCommand]
        async void DeleteItem(DataLibraryItem ItemToDelete)
        {
            bool confirm = await Shell.Current.CurrentPage.DisplayAlert("Confirm Deletion", $"{ItemToDelete.MeasurementName}\n{ItemToDelete.LocalTimestamp}", "Delete", "Cancel");
            if (!confirm)
            {
                return;
            }
            DataFile.DeleteDataRecord(ItemToDelete.LocalTimestamp);

            RefreshLibraryItemsList();
        }

        [RelayCommand(CanExecute = nameof(MeasurementsExist))]
        void EmailDataFile()
        {
            DataFile.EmailDataFile();
        }

        [RelayCommand(CanExecute = nameof(MeasurementsExist))]
        void EditList()
        {
            EditListButtonText = ShowDeleteMeasurementsButtons ? "Edit List" : "Done";
            ShowDeleteMeasurementsButtons = !ShowDeleteMeasurementsButtons;
        }

        [RelayCommand]
        async void MissingMeasurements()
        {

            string messageBody;

#if ANDROID
                messageBody = "HydroColor version 2.0 was a major update to how the app collects and stores data. Measurements collected with HydroColor versions before 2.0 are no longer viewable in the app. Don’t panic! The data file containing all your measurements is still available on your device. \n\nYou can download the datafile containing your past measurements by connecting your Android phone to a computer. The HydroColor data files are available under <your phone>\\Android\\data\\com.h2optics.hydrocolor\\files\\.";
#endif

#if IOS
            messageBody = "HydroColor version 2.0 was a major update to how the app collects and stores data. Measurements collected with HydroColor versions before 2.0 are no longer viewable in the app. Don’t panic! The data file containing all your measurements is still available on your device. \n\nYou can download the datafile containing your past measurements by connecting your iPhone to a computer. On Mac, the HydroColor data files are available under the ‘Files’ tab. On a PC, you must connect the iPhone with iTunes, then the data file is available under ‘File Sharing’.";
#endif

            await Shell.Current.CurrentPage.DisplayAlert("Missing Data", messageBody , "OK");

        }
    }
}
