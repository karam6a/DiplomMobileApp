// Pages/MainPage.xaml.cs
using LogisticMobileApp.Helpers;
using LogisticMobileApp.ViewModels;
using Microsoft.Maui.Graphics;
using System.Globalization;

namespace LogisticMobileApp.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // ← ВАЖНО: обновляем язык при появлении страницы
        UpdateLanguage();

        var drawable = new CubeDrawable();
        CubeGraphicsView.Drawable = drawable;

        // ВРАЩЕНИЕ
        CubeGraphicsView.RotateTo(360, 2000, Easing.Linear)
            .ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    while (true)
                    {
                        CubeGraphicsView.Rotation = 0;
                        await CubeGraphicsView.RotateTo(360, 2000, Easing.Linear);
                    }
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());

        // ПУЛЬСАЦИЯ (дыхание)
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            while (true)
            {
                await CubeGraphicsView.ScaleTo(1.12, 1200, Easing.SinInOut);
                await CubeGraphicsView.ScaleTo(1.0, 1200, Easing.SinInOut);
            }
        });

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateLanguage(); // ← каждый раз при показе страницы
    }

    private void UpdateLanguage()
    {
        var lang = Preferences.Get("Language", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        LocalizationResourceManager.Instance.SetCulture(new CultureInfo(lang));
    }
}

