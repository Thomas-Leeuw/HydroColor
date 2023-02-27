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
    }
}
