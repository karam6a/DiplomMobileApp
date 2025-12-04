using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using LogisticMobileApp.Models;

namespace LogisticMobileApp.ViewModels
{
    public class ConfirmRouteViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RouteStop> Stops { get; }

        /// <summary>
        /// Кнопка «Завершить маршрут» должна быть активна, когда все точки либо подтверждены, либо отклонены.
        /// </summary>
        public bool AllFinished => Stops?.Count > 0 && Stops.All(s => s.IsConfirmed || s.IsRejected);

        public ConfirmRouteViewModel(List<ClientData> clientsData)
        {
            // Преобразуем ClientData в RouteStop для экрана подтверждения
            Stops = new ObservableCollection<RouteStop>(
                (clientsData ?? new List<ClientData>()).Select(c => new RouteStop
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    ContainerCount = c.ContainerCount
                }));

            // следим за изменениями внутри элементов, чтобы обновлять AllFinished
            foreach (var s in Stops)
                SubscribeStop(s);

            Stops.CollectionChanged += Stops_CollectionChanged;
        }

        private void Stops_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (RouteStop s in e.NewItems) SubscribeStop(s);

            if (e.OldItems != null)
                foreach (RouteStop s in e.OldItems) UnsubscribeStop(s);

            OnPropertyChanged(nameof(AllFinished));
        }

        private void SubscribeStop(RouteStop stop)
        {
            stop.PropertyChanged += Stop_PropertyChanged;
        }

        private void UnsubscribeStop(RouteStop stop)
        {
            stop.PropertyChanged -= Stop_PropertyChanged;
        }

        private void Stop_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Любое изменение статусов приводит к пересчёту AllFinished
            if (e.PropertyName == nameof(RouteStop.IsConfirmed) ||
                e.PropertyName == nameof(RouteStop.IsRejected))
            {
                OnPropertyChanged(nameof(AllFinished));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
