using Android.App;
using Android.Runtime;

namespace HydroColor;

[Application]
// The Google Maps key is written into the app manifest at build time from Secrets (see Secrets.template.cs).
[MetaData("com.google.android.geo.API_KEY", Value = Secrets.GoogleMapsApiKey)]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
