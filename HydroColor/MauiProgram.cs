using CommunityToolkit.Maui;
using HydroColor.Controls;
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
                handlers.AddHandler<CameraPreview, Handlers.CameraPreviewHandler>();

#if IOS
                // A custom shell that switches tabs without animation on iOS (see CustomShell.cs).
                handlers.AddHandler<Shell, Platforms.iOS.CustomShell>();
#endif
            });


        builder.Services.AddSingleton<CollectDataView, CollectDataViewModel>();
        builder.Services.AddSingleton<LibraryView, LibraryViewModel>();
        builder.Services.AddSingleton<AboutView, AboutViewModel>();
        builder.Services.AddTransient<CaptureImageView, CaptureImageViewModel>();
        builder.Services.AddTransient<DataView, DataViewModel>();
        builder.Services.AddTransient<WelcomeView, WelcomeViewModel>();
        builder.Services.AddTransient<SensitivityTestView, SensitivityTestViewModel>();

        return builder.Build();
    }
}
