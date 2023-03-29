
using HydroColor.Resources.Strings;

namespace HydroColor.Services
{
    public class HardwarePermissions
    {
        public static async Task<PermissionStatus> RequestPermission<T>() where T : Permissions.BasePermission, new()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<T>();

            switch (status)
            {
                case PermissionStatus.Granted:

                    break;

                case PermissionStatus.Denied:

                    if (DeviceInfo.Platform == DevicePlatform.iOS)
                    {
                        await RequestManualPermissionActivation<T>();
                        // On iOS, once a permission has been denied it may not be requested again from the application   
                    }
                    else
                    {
                        // Prompt for permission again on Android
                        status = await Permissions.RequestAsync<T>();
                        if (status == PermissionStatus.Denied)
                        {
                            await RequestManualPermissionActivation<T>();
                        }
                    }
                    break;

                default: 
                    // 'Unknown' state will land here, usually on the first time permission has been requested
                    status = await Permissions.RequestAsync<T>();
                    break;
            }
            return status;
        }

        private static async Task RequestManualPermissionActivation<T>()
        {
            if (typeof(T) == typeof(Permissions.Camera))
            {
                await Shell.Current.CurrentPage.DisplayAlert(Strings.HardwarePermissions_CameraPermissionTitle, Strings.HardwarePermissions_CameraPermissionMessage, Strings.HardwarePermissions_CameraPermissionDismissButton);

            }
        }

    }
}
