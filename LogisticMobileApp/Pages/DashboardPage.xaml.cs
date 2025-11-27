using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Storage;
using LogisticMobileApp.Helpers;
using LogisticMobileApp.Resources.Strings;
using LogisticMobileApp.ViewModels;

namespace LogisticMobileApp.Pages
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Точно так же, как на MainPage — обновляем язык при появлении
            UpdateLanguage();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            var lang = Preferences.Get("Language", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(lang));
        }

        // Переход на страницу выбора точек
        private async void OnRouteClicked(object sender, EventArgs e)
        {
            var routesPage = App.Services.GetRequiredService<RoutesPage>();
            await Navigation.PushAsync(routesPage);
        }

        // Заглушка статистики
        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            await DisplayAlert(AppResources.Statistics_Button,
                               "Статистика (логика не реализована)",
                               "OK");
        }

        // Кнопка выхода
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert(
                LocalizationResourceManager.Instance["Logout_Title"],
                LocalizationResourceManager.Instance["Logout_ConfirmMessage"],
                LocalizationResourceManager.Instance["Logout_Yes"],
                LocalizationResourceManager.Instance["Logout_No"]);

            if (!answer) return;

#if ANDROID
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif IOS
            // iOS не позволяет закрыть приложение программно
            System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
#elif WINDOWS || MACCATALYST
            Application.Current.Quit();
#else
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
