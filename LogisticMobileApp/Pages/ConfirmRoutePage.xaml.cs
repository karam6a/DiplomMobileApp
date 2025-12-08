// ConfirmRoutePage.xaml.cs
using System.Windows.Input;
using Microsoft.Maui.Controls;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.ApplicationModel;

namespace LogisticMobileApp.Pages
{
    public partial class ConfirmRoutePage : ContentPage
    {
        public ICommand ConfirmCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand SendCommentCommand { get; }
        private readonly ApiService _apiService;
        private readonly List<ClientData> _clientsData;

        public ConfirmRoutePage(ApiService apiService, List<ClientData> clientsData)
        {
            InitializeComponent();
            _apiService = apiService;
            _clientsData = clientsData;

            ConfirmCommand = new Command<RouteStop>(stop =>
            {
                if (stop == null) return;
                stop.IsConfirmed = true;
                stop.IsRejected = false;
                stop.Comment = string.Empty;
            });

            RejectCommand = new Command<RouteStop>(stop =>
            {
                if (stop == null) return;
                stop.IsRejected = true;
                stop.IsConfirmed = false;
            });

            SendCommentCommand = new Command<RouteStop>(async stop =>
            {
                if (stop == null) return;
                await DisplayAlert("Комментарий отправлен",
                    $"{stop.Name}\n{(string.IsNullOrWhiteSpace(stop.Comment) ? "(пусто)" : stop.Comment)}",
                    "OK");
            });

            BindingContext = new ConfirmRouteViewModel(clientsData);
        }

        private async void OnFinishRouteClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await _apiService.EndRouteAsync();
                if (result)
                {
                    Preferences.Set("RouteStarted", false);
                    Preferences.Remove("RouteId");
                    Preferences.Remove("RouteStartTime");

                    await Toast.Make("Маршрут завершён!", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnMapToolbarItemClicked(object sender, EventArgs e)
        {
            if (_clientsData == null || _clientsData.Count == 0)
            {
                await DisplayAlert("Ошибка", "Нет данных о клиентах", "OK");
                return;
            }

            try
            {
                var coordinates = new List<string>();
                foreach (var client in _clientsData)
                {
                    if (string.IsNullOrWhiteSpace(client.Coordinates))
                        continue;

                    var parts = client.Coordinates.Split(',');
                    if (parts.Length >= 2 &&
                        double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                        double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lon))
                    {
                        coordinates.Add($"{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    }
                }

                OpenGoogleMapsRoute(coordinates);

                if (coordinates.Count < 2)
                {
                    await DisplayAlert("Ошибка", "Недостаточно точек с координатами для построения маршрута", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть маршрут: {ex.Message}", "OK");
            }
        }

        private async void OpenGoogleMapsRoute(List<string> coordinates)
        {
            if (coordinates == null || coordinates.Count < 2)
            {
                await DisplayAlert("Ошибка", "Нужно минимум 2 точки", "OK");
                return;
            }

            string origin = coordinates.First();
            string destination = coordinates.Last();

            // Промежуточные точки, если есть
            string waypoints = string.Join("|", coordinates.Skip(1).Take(coordinates.Count - 2));

            // Формируем URL
            string url = $"https://www.google.com/maps/dir/?api=1" +
                         $"&origin={origin}" +
                         $"&destination={destination}";

            if (!string.IsNullOrWhiteSpace(waypoints))
                url += $"&waypoints={waypoints}";

            // Открываем Google Maps
            await Launcher.OpenAsync(new Uri(url));
        }

    }
}