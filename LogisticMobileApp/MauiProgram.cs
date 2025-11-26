using Microsoft.Extensions.Logging;
using LogisticMobileApp.Helpers;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Pages;
using ZXing.Net.Maui.Controls;
using LogisticMobileApp.Services;



namespace LogisticMobileApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseBarcodeReader()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
				fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FA");
			});
        builder.Services.AddSingleton(LocalizationResourceManager.Instance);
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RoutesViewModel>();
        builder.Services.AddTransient<RoutesPage>();
		builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();



#if DEBUG
        builder.Logging.AddDebug();
#endif

		var app = builder.Build();
        return app;
    }
}
