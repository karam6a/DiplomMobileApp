using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;                 // для Command и Shell
using LogisticMobileApp.Models;                // ClientItem, RouteStop

namespace LogisticMobileApp.ViewModels
{
    public class ConfirmRouteViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RouteStop> Stops { get; }

        /// <summary>
        /// Кнопка «Завершить маршрут» должна быть активна, когда все точки либо подтверждены, либо отклонены.
        /// </summary>
        public bool AllFinished => Stops?.Count > 0 && Stops.All(s => s.IsConfirmed || s.IsRejected);

        public ICommand FinishRouteCommand { get; }

        public ConfirmRouteViewModel(System.Collections.Generic.IEnumerable<ClientItem> selected)
        {
            // Преобразуем выбранные ClientItem в RouteStop для экрана подтверждения
            Stops = new ObservableCollection<RouteStop>(
                (selected ?? Array.Empty<ClientItem>()).Select(c => new RouteStop
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

            FinishRouteCommand = new Command(async () =>
            {
                // Навигация на главную (рабочая панель)
                await Shell.Current.GoToAsync("//DashboardPage");
            });
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
