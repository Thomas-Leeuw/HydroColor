using CommunityToolkit.Maui;
using HydroColor.ViewModels;
using HydroColor.Views;
using Microsoft.Maui.Controls.Compatibility.Hosting;

namespace HydroColor;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			}).UseMauiCompatibility()
			.ConfigureMauiHandlers((handlers) => {

#if ANDROID
                handlers.AddCompatibilityRenderer(typeof(CameraPreview), typeof(HydroColor.Platforms.Android.CameraPreviewRenderer));
#endif

#if IOS
				handlers.AddHandler(typeof(CameraPreview), typeof(HydroColor.Platforms.iOS.CameraPreviewRenderer));
#endif
            });

        builder.Services.AddSingleton<CollectDataViewModel>();
        builder.Services.AddSingleton<CollectDataView>();

        builder.Services.AddSingleton<LibraryViewModel>();
        builder.Services.AddSingleton<LibraryView>();

        builder.Services.AddSingleton<AboutViewModel>();
        builder.Services.AddSingleton<AboutView>();

		builder.Services.AddTransient<CaptureImageViewModel>();
		builder.Services.AddTransient<CaptureImageView>();

        builder.Services.AddTransient<DataViewModel>();
        builder.Services.AddTransient<DataView>();

		builder.Services.AddTransient<WelcomeViewModel>();
		builder.Services.AddTransient<WelcomeView>();

        return builder.Build();
	}
}
