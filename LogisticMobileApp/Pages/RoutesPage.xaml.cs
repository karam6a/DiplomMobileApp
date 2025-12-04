using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Models;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;

namespace LogisticMobileApp.Pages
{
    public partial class RoutesPage : ContentPage
    {
        public RoutesPage(RoutesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is RoutesViewModel vm)
                await vm.LoadAsync();
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
