using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Windows.Input;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using Microsoft.Maui.ApplicationModel;
using LogisticMobileApp.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LogisticMobileApp.ViewModels
{
    public partial class RoutesViewModel : ObservableObject
    {
        private readonly ApiService _api = new();

        public ObservableCollection<ClientItem> Clients { get; } = new();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool HasSelected => Clients.Any(c => c.IsSelected);

       // public ICommand BuildRouteCommand { get; }

        public RoutesViewModel()
        {
            //BuildRouteCommand = new Command(async () => await BuildRouteAsync().ConfigureAwait(false));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                Clients.Clear();
                var items = await _api.GetClientsAsync(ct);

                foreach (var c in items)
                {
                    c.SelectionChanged += (_, __) => OnPropertyChanged(nameof(HasSelected));
                    Clients.Add(c);
                }

                OnPropertyChanged(nameof(HasSelected));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task BuildRoute()
        {
            var selected = Clients.Where(c => c.IsSelected).ToList();
            if (!selected.Any()) return;

            // навигация в ConfirmRoutePage
            await Shell.Current.Navigation.PushAsync(new ConfirmRoutePage(selected));

            // формирование URL карты
            var points = selected.Select(c =>
                string.Format(CultureInfo.InvariantCulture, "{0},{1}", c.Lat, c.Lon));

            var url = "https://www.google.com/maps/dir/" + string.Join("/", points);

            try
            {
                await Launcher.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}
