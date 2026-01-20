using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;

namespace LogisticMobileApp.Pages
{
    public partial class RoutesPage : ContentPage
    {
        private readonly RouteHubService _hubService;
        private readonly ApiService _apiService;

        public RoutesPage(RoutesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _hubService = App.Services.GetRequiredService<RouteHubService>();
            _apiService = App.Services.GetRequiredService<ApiService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Подписываемся на обновления маршрута
            _hubService.OnRouteUpdated += HandleRouteUpdated;

            if (BindingContext is RoutesViewModel vm)
                await vm.LoadAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Отписываемся от обновлений
            _hubService.OnRouteUpdated -= HandleRouteUpdated;
        }

        private async void HandleRouteUpdated(RouteUpdatedDto data)
        {
            try
            {
                // Загружаем свежие данные маршрута с сервера
                var route = await _apiService.GetMyRouteAsync();
                if (route?.ClientsData != null && BindingContext is RoutesViewModel vm)
                {
                    // Обновляем данные в ViewModel
                    vm.SetClientsData(route.ClientsData, route.RouteName ?? vm.RouteName);
                    await vm.LoadAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoutesPage] HandleRouteUpdated error: {ex.Message}");
            }
        }

        private async void OnMapIconTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ClientData client)
            {
                if (string.IsNullOrWhiteSpace(client.Coordinates))
                {
                    await DisplayAlert("Ошибка", "Координаты не указаны", "OK");
                    return;
                }

                try
                {
                    // Парсим координаты (формат: "lat,lon")
                    var address = client.City + " " + client.Address;
                    if (String.IsNullOrEmpty(address))
                    {
                        await DisplayAlert("Ошибка", "Неверный формат адреса", "OK");
                        return;
                    }

                    // Формируем URL для Google Maps
                    var url = $"https://www.google.com/maps?q={address}";

                    // Открываем Google Maps
                    await Launcher.OpenAsync(new Uri(url));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось открыть карту: {ex.Message}", "OK");
                }
            }
        }
    }
}
