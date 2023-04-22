using CommunityToolkit.Maui.Views;
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
        bool showDeleteMeasurementsButtons;

        [ObservableProperty]
        string editListButtonText = Strings.Library_EditListButton;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EmailDataFileCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditListCommand))]
        bool measurementsExist;

        FileReaderWriter DataFile = new();

        [RelayCommand]
        void ViewAppearing()
        {
            EditListButtonText = Strings.Library_EditListButton;
            ShowDeleteMeasurementsButtons = false;
            RefreshLibraryItemsList();
        }

        void RefreshLibraryItemsList()
        {
            List<DataLibraryItem> DataItems = DataFile.LoadDataSummary();

            MeasurementsExist = DataItems.Count > 0;
            if (!MeasurementsExist)
            {
                EditListButtonText = Strings.Library_EditListButton;
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
            bool confirm = await Shell.Current.CurrentPage.DisplayAlert(Strings.Library_DeleteMeasurementTitle, $"{ItemToDelete.MeasurementName}\n{ItemToDelete.LocalTimestamp}", Strings.Library_DeleteMeasurementDeleteButton, Strings.Library_DeleteMeasurementCancelButton);
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
            EditListButtonText = ShowDeleteMeasurementsButtons ? Strings.Library_EditListButton : Strings.Library_EditListDoneButton;
            ShowDeleteMeasurementsButtons = !ShowDeleteMeasurementsButtons;
        }

        [RelayCommand]
        async void MissingMeasurements()
        {

            string messageBody;

#if ANDROID
                messageBody = Strings.Library_MissingMeasurmentsMessageAndroid;
#endif

#if IOS
            messageBody = Strings.Library_MissingMeasurmentsMessageIOS;
#endif

            await Shell.Current.CurrentPage.DisplayAlert(Strings.Library_MissingMeasurmentsTitle, messageBody , Strings.Library_MissingMeasurmentsDismissButtion);

        }
    }
}
