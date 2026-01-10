// ConfirmRoutePage.xaml.cs
using System.Windows.Input;
using Microsoft.Maui.Controls;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.ApplicationModel;
using LogisticMobileApp.Resources.Strings;

namespace LogisticMobileApp.Pages
{
    public partial class ConfirmRoutePage : ContentPage
    {
        public ICommand ConfirmCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand SendCommentCommand { get; }
        private readonly ApiService _apiService;
        private readonly List<ClientData> _clientsData;
        private readonly string? _geometryJson;

        public ConfirmRoutePage(ApiService apiService, List<ClientData> clientsData, string? geometryJson = null)
        {
            InitializeComponent();
            _apiService = apiService;
            _clientsData = clientsData;
            _geometryJson = geometryJson;

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

                try
                {
                    var result = await _apiService.AddNoteAsync(stop.Id, stop.Comment ?? string.Empty);
                    if (result)
                    {
                        await Toast.Make(AppResources.ConfirmRoute_SendComment + " ✓", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();

                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
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

                    Preferences.Set("NoActiveRoute", true);

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
                // Открываем встроенную карту с готовым маршрутом
                var mapPage = new MapPage(_clientsData, _geometryJson);
                await Navigation.PushAsync(mapPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось открыть карту: {ex.Message}", "OK");
            }
        }

    }
}