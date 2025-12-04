using Microsoft.Extensions.Logging;
using LogisticMobileApp.Helpers;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Pages;
using ZXing.Net.Maui.Controls;
using LogisticMobileApp.Services;
using CommunityToolkit.Maui;


#if ANDROID
using LogisticMobileApp.Platforms.Android.Helpers;
#endif



namespace LogisticMobileApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseBarcodeReader()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
				fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FA");
                fonts.AddFont("RoadNumbers.otf", "BelarusGOST");

            });
        builder.Services.AddSingleton(LocalizationResourceManager.Instance);
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RoutesViewModel>();
        builder.Services.AddTransient<RoutesPage>();
		builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DashboardPage>();
#if ANDROID
        builder.Services.AddSingleton<IDeviceHelper, DeviceHelper>();
#else
        builder.Services.AddSingleton<IDeviceHelper>(sp => throw new NotSupportedException());
#endif


#if DEBUG
        builder.Logging.AddDebug();
#endif

		var app = builder.Build();
        return app;
    }
}
