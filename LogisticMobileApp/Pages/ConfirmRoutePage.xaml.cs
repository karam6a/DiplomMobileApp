using System.Windows.Input;
using Microsoft.Maui.Controls;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using CommunityToolkit.Maui.Alerts;

namespace LogisticMobileApp.Pages
{
    public partial class ConfirmRoutePage : ContentPage
    {
        public ICommand ConfirmCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand SendCommentCommand { get; }

        private readonly ApiService _apiService;

        public ConfirmRoutePage(List<ClientData> clientsData)
        {
            InitializeComponent();

            _apiService = App.Services.GetRequiredService<ApiService>();

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
                    // Очищаем информацию о маршруте в Preferences
                    Preferences.Set("RouteStarted", false);
                    Preferences.Remove("RouteId");
                    Preferences.Remove("RouteStartTime");

                    // Показываем тост
                    await Toast.Make("Маршрут завершён!", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();

                    // Переходим на Dashboard
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}
