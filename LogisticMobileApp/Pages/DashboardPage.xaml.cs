using CommunityToolkit.Maui.Alerts;
using LogisticMobileApp.Helpers;
using LogisticMobileApp.Resources.Strings;
using LogisticMobileApp.Services;
using LogisticMobileApp.Services.LocationStreaming;
using LogisticMobileApp.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Storage;
using System;
using System.ComponentModel;
using System.Globalization;

namespace LogisticMobileApp.Pages
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel ViewModel;
        private readonly ApiService _apiService;
        private readonly RouteHubService _hubService;
        private readonly GpsStreamingService _gpsService;
        private readonly IBackgroundService _backgroundService;
        public DashboardPage(DashboardViewModel viewModel, ApiService apiService, RouteHubService hubService, GpsStreamingService gps, IBackgroundService backgroundService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            ViewModel = viewModel;
            _apiService = apiService;
            _hubService = hubService;
            _gpsService = gps;
            UpdateLanguage();
            _backgroundService = backgroundService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateLanguage();
            
            // Перезагружаем данные при возврате на страницу
            await ViewModel.ReloadAsync();

            // Запускаем SignalR подключение (если ещё не подключены)
            await StartSignalRAsync();

            if (!string.IsNullOrEmpty(ViewModel.DriverName))
            {
                // 1. Сначала просим все разрешения
                bool hasPermissions = await CheckAndRequestPermissions();

                // 2. Если пользователь отказал, не запускаем сервис, иначе будет краш
                if (!hasPermissions)
                {
                    await DisplayAlert("Ошибка", "Без доступа к геолокации приложение не может работать.", "OK");
                    return;
                }

                // 3. Отключаем оптимизацию батареи (чтобы не убило через 5 минут)
                await RequestBatteryOptimization();

                // 4. Запускаем сервис
                _backgroundService.Start(ViewModel.DriverName, ViewModel.LicensePlate);
            }
        }

        




        private async Task<bool> CheckAndRequestPermissions()
        {
            // 1. Разрешение на УВЕДОМЛЕНИЯ (Android 13+)
            // Без этого Foreground Service не запустится
            if (Permissions.ShouldShowRationale<Permissions.PostNotifications>())
            {
                await DisplayAlert("Разрешение", "Приложению нужны уведомления, чтобы показывать статус работы GPS.", "OK");
            }

            var statusNotif = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (statusNotif != PermissionStatus.Granted)
            {
                statusNotif = await Permissions.RequestAsync<Permissions.PostNotifications>();
            }

            // 2. Разрешение на ГЕОЛОКАЦИЮ (При использовании)
            // Это база для работы GPS
            var statusGeo = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (statusGeo != PermissionStatus.Granted)
            {
                // Если нужно, показываем объяснение
                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    await DisplayAlert("Геолокация", "Нам нужен доступ к GPS для отслеживания маршрута.", "Понятно");
                }
                statusGeo = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            // 3. Разрешение на ГЕОЛОКАЦИЮ (Всегда/В фоне) - Опционально, но желательно для Android 10+
            // Android может потребовать отдельного подтверждения для "Always Allow"
            var statusBackground = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (statusBackground != PermissionStatus.Granted)
            {
                // Обычно сначала просят WhenInUse, а потом Always. 
                // Можно попробовать запросить, но если откажут - сервис все равно будет работать как Foreground Service
                await Permissions.RequestAsync<Permissions.LocationAlways>();
            }

            // Возвращаем true, если базовое разрешение на GPS получено
            return statusGeo == PermissionStatus.Granted;
        }

        private async Task RequestBatteryOptimization()
        {
#if ANDROID
            try
            {
                var pm = (Android.OS.PowerManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.PowerService);
                var packageName = Android.App.Application.Context.PackageName;

                if (!pm.IsIgnoringBatteryOptimizations(packageName))
                {
                    bool result = await DisplayAlert("Настройка", "Отключите экономию заряда для стабильной работы GPS в фоне.", "Настройки", "Отмена");
                    if (result)
                    {
                        var intent = new Android.Content.Intent();
                        intent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                        intent.SetData(Android.Net.Uri.Parse("package:" + packageName));
                        intent.SetFlags(Android.Content.ActivityFlags.NewTask);
                        Platform.CurrentActivity.StartActivity(intent);
                    }
                }
            }
            catch { }
#endif
            await Task.CompletedTask;
        }


        

        private void UpdateLanguage()
        {
            var lang = Preferences.Get("Language", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            LocalizationResourceManager.Instance.SetCulture(new CultureInfo(lang));
        }

        #region SignalR

        private async Task StartSignalRAsync()
        {
            try
            {
                // Подписываемся на событие для обновления данных
                // (тост показывается автоматически в RouteHubService)
                _hubService.OnRouteUpdated -= HandleRouteUpdated;
                _hubService.OnRouteUpdated += HandleRouteUpdated;

                // Запускаем подключение если отключены
                if (_hubService.State == HubConnectionState.Disconnected)
                {
                    await _hubService.StartAsync();
                    System.Diagnostics.Debug.WriteLine("[Dashboard] SignalR connected");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] SignalR connection failed: {ex.Message}");
                // Не показываем ошибку пользователю — приложение работает и без SignalR
            }
        }

        private async void HandleRouteUpdated(RouteUpdatedDto data)
        {
            // Тост уже показан автоматически в RouteHubService
            // Здесь только перезагружаем данные маршрута
            await ViewModel.ReloadAsync();
        }

        #endregion

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

        // Выбор языка через ActionSheet
        private async void OnLanguageButtonClicked(object sender, EventArgs e)
        {
            var title = LocalizationResourceManager.Instance["Language_PromptTitle"];
            var cancel = LocalizationResourceManager.Instance["Language_Cancel"];

            var choice = await DisplayActionSheet(
                title,
                cancel,
                null,
                "ru", "en", "pl");

            if (string.IsNullOrEmpty(choice) || choice == cancel)
                return;

            try
            {
                var culture = new CultureInfo(choice);
                LocalizationResourceManager.Instance.SetCulture(culture);
                Preferences.Set("Language", choice);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    LocalizationResourceManager.Instance["Settings_Title"],
                    ex.Message,
                    "OK");
            }
        }

        private void OnThemeButtonClicked(object sender, EventArgs e)
        {
            // Переключаем тему
            if (Application.Current.UserAppTheme == AppTheme.Dark)
            {
                Application.Current.UserAppTheme = AppTheme.Light;
                Preferences.Set("AppTheme", "Light");
            }
            else
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
                Preferences.Set("AppTheme", "Dark");
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
