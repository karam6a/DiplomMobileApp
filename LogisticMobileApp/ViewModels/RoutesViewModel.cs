using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LogisticMobileApp.Models;

namespace LogisticMobileApp.ViewModels
{
    public partial class RoutesViewModel : ObservableObject
    {
        public ObservableCollection<ClientData> Clients { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string routeName = string.Empty;

        private List<ClientData> _clientsData = new();

        public RoutesViewModel()
        {
        }

        public void SetClientsData(List<ClientData> clientsData, string name)
        {
            _clientsData = clientsData ?? new List<ClientData>();
            RouteName = name;
        }

        public Task LoadAsync(CancellationToken ct = default)
        {
            if (IsBusy) return Task.CompletedTask;

            IsBusy = true;
            try
            {
                Clients.Clear();

                foreach (var client in _clientsData)
                {
                    Clients.Add(client);
                }
            }
            finally
            {
                IsBusy = false;
            }

            return Task.CompletedTask;
        }
    }
}
