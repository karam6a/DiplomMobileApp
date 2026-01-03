using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Storage;
using LogisticMobileApp.Helpers;
using LogisticMobileApp.Resources.Strings;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Services;
using CommunityToolkit.Maui.Alerts;

namespace LogisticMobileApp.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel ViewModel;
        private readonly ApiService _apiService;

        public DashboardPage(DashboardViewModel viewModel, ApiService apiService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            ViewModel = viewModel;
            _apiService = apiService;
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

        // Переход на страницу списка клиентов маршрута
        private async void OnShowRouteClicked(object sender, EventArgs e)
        {
            if (ViewModel.MyRouteInfo?.ClientsData == null || ViewModel.MyRouteInfo.ClientsData.Count == 0)
            {
                await DisplayAlert("Ошибка", "Маршрут не загружен или нет клиентов", "OK");
                return;
            }

            var routesPage = App.Services.GetRequiredService<RoutesPage>();
            var routesViewModel = App.Services.GetRequiredService<RoutesViewModel>();
            
            routesViewModel.SetClientsData(ViewModel.MyRouteInfo.ClientsData, ViewModel.Name);
            routesPage.BindingContext = routesViewModel;
            
            await Navigation.PushAsync(routesPage);
        }

        // Начать или продолжить маршрут
        private async void OnStartRouteClicked(object sender, EventArgs e)
        {
            if (ViewModel.MyRouteInfo?.ClientsData == null || ViewModel.MyRouteInfo.ClientsData.Count == 0)
            {
                await DisplayAlert("Ошибка", "Маршрут не загружен или нет клиентов", "OK");
                return;
            }

            // Если маршрут уже начат — просто переходим на страницу подтверждения
            if (ViewModel.IsRouteStarted)
            {
                var confirmPage = new ConfirmRoutePage(_apiService, ViewModel.MyRouteInfo.ClientsData, ViewModel.MyRouteInfo.GeometryJson);
                await Navigation.PushAsync(confirmPage);
                return;
            }

            // Иначе отправляем запрос на начало маршрута
            try
            {
                var result = await _apiService.StartRouteAsync();
                if (result)
                {
                    // Сохраняем информацию о начале маршрута в Preferences
                    Preferences.Set("RouteStarted", true);
                    Preferences.Set("RouteId", ViewModel.MyRouteInfo.Id);
                    Preferences.Set("RouteStartTime", DateTime.Now.ToString("O")); // ISO 8601 формат

                    // Обновляем состояние
                    ViewModel.IsRouteStarted = true;
                    ViewModel.StartButtonText = "Продолжить";

                    // Показываем тост
                    await Toast.Make(AppResources.StartRouteToast, CommunityToolkit.Maui.Core.ToastDuration.Short).Show();

                    // Переходим на страницу подтверждения
                    var confirmPage = new ConfirmRoutePage(_apiService, ViewModel.MyRouteInfo.ClientsData, ViewModel.MyRouteInfo.GeometryJson);
                    await Navigation.PushAsync(confirmPage);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

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
            // iOS �� ��������� ������� ���������� ����������
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
