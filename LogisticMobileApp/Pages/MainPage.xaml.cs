// Pages/MainPage.xaml.cs
using LogisticMobileApp.ViewModels;
using Microsoft.Maui.Graphics;

namespace LogisticMobileApp.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

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
}

