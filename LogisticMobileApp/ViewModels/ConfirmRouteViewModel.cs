using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;

namespace LogisticMobileApp.ViewModels
{
    public class ConfirmRouteViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RouteStop> Stops { get; }
        
        private readonly List<ClientData> _clientsData;
        private readonly PickUpStatusService _pickUpStatusService;
        private readonly int _routeId;

        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Кнопка «Завершить маршрут» должна быть активна, когда все точки либо подтверждены, либо отклонены.
        /// </summary>
        public bool AllFinished => Stops?.Count > 0 && Stops.All(s => s.IsConfirmed || s.IsRejected);

        public ConfirmRouteViewModel(List<ClientData> clientsData)
        {
            _clientsData = clientsData ?? new List<ClientData>();
            _pickUpStatusService = new PickUpStatusService();
            _routeId = Preferences.Get("RouteId", 0);
            
            Stops = new ObservableCollection<RouteStop>();
            Stops.CollectionChanged += Stops_CollectionChanged;
            
            // НЕ загружаем данные в конструкторе - загрузка будет в OnAppearing
        }
        
        /// <summary>
        /// Асинхронно загружает точки с учётом локальных статусов
        /// </summary>
        public async Task LoadStopsAsync()
        {
            IsLoading = true;
            
            try
            {
                // Получаем сохранённые статусы
                var statuses = await _pickUpStatusService.GetRouteStatusesAsync(_routeId);
                var statusDict = statuses.ToDictionary(s => s.ClientId);
                
                // Создаём список RouteStop с применением статусов
                var allStops = _clientsData.Select((c, index) => new RouteStop
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    ContainerCount = c.ContainerCount,
                    OriginalIndex = index + 1,
                    IsConfirmed = statusDict.TryGetValue(c.Id, out var status) && status.IsConfirmed,
                    IsRejected = statusDict.TryGetValue(c.Id, out status) && status.IsRejected,
                    Comment = statusDict.TryGetValue(c.Id, out status) ? status.Comment : null
                }).ToList();
                
                // Сортируем: сначала необработанные, потом обработанные
                var sortedStops = allStops
                    .OrderBy(s => s.IsConfirmed || s.IsRejected) // false < true
                    .ThenBy(s => s.OriginalIndex)
                    .ToList();
                
                // Очищаем и заполняем коллекцию
                Stops.Clear();
                foreach (var stop in sortedStops)
                {
                    SubscribeStop(stop);
                    Stops.Add(stop);
                }
                
                OnPropertyChanged(nameof(AllFinished));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmRouteViewModel] LoadStopsAsync error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
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
