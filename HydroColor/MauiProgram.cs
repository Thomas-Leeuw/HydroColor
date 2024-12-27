using CommunityToolkit.Maui;
using HydroColor.ViewModels;
using HydroColor.Views;

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
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler(typeof(CameraPreview), typeof(Handlers.CameraPreviewHandler));

#if IOS
                // Added a custom shell to disable animation when switching tabs.
                // The animation was causing a flicker when switching tabs on iOS.
                handlers.AddHandler(typeof(Shell), typeof(HydroColor.Platforms.iOS.CustomShell));
#endif
            });


        builder.Services.AddSingleton<CollectDataView, CollectDataViewModel>();
        builder.Services.AddSingleton<LibraryView, LibraryViewModel>();
        builder.Services.AddSingleton<AboutView, AboutViewModel>();
        builder.Services.AddTransient<CaptureImageView, CaptureImageViewModel>();
        builder.Services.AddTransient<DataView, DataViewModel>();
        builder.Services.AddTransient<WelcomeView, WelcomeViewModel>();

        return builder.Build();
    }
}
