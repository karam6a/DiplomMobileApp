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
        public ICommand CancelRejectCommand { get; }
        private readonly ApiService _apiService;
        private readonly RouteHubService _hubService;
        private readonly List<ClientData> _clientsData;
        private readonly string? _geometryJson;
        private readonly PickUpStatusService _pickUpStatusService;
        private readonly int _routeId;
        private ConfirmRouteViewModel _viewModel;

        public ConfirmRoutePage(ApiService apiService, List<ClientData> clientsData, string? geometryJson = null)
        {
            InitializeComponent();
            _apiService = apiService;
            _hubService = App.Services.GetRequiredService<RouteHubService>();
            _clientsData = clientsData;
            _geometryJson = geometryJson;
            _pickUpStatusService = new PickUpStatusService();
            _routeId = Preferences.Get("RouteId", 0);

            ConfirmCommand = new Command<RouteStop>(async stop =>
            {
                if (stop == null) return;
                stop.IsConfirmed = true;
                stop.IsRejected = false;
                stop.Comment = string.Empty;
                
                // Отправляем статус на сервер
                try
                {
                    await _apiService.UpdateProcessingStatusAsync(stop.Id, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConfirmRoutePage] Failed to update processing status: {ex.Message}");
                }
                
                // Сохраняем статус локально
                await _pickUpStatusService.ConfirmAsync(stop.Id, _routeId);
                
                // Пересортируем список
                await ReorderStopsAsync();
            });

            RejectCommand = new Command<RouteStop>(async stop =>
            {
                if (stop == null) return;
                stop.IsRejected = true;
                stop.IsConfirmed = false;
                
                // Сохраняем статус локально (статус на сервер отправится при SendCommentCommand)
                await _pickUpStatusService.RejectAsync(stop.Id, _routeId, stop.Comment);
            });

            SendCommentCommand = new Command<RouteStop>(async stop =>
            {
                if (stop == null) return;

                try
                {
                    // Отправляем комментарий
                    var result = await _apiService.AddNoteAsync(stop.Id, stop.Comment ?? string.Empty);
                    if (result)
                    {
                        // Отправляем статус на сервер (отклонено = false)
                        await _apiService.UpdateProcessingStatusAsync(stop.Id, false);
                        
                        // Обновляем локальный статус с комментарием
                        await _pickUpStatusService.RejectAsync(stop.Id, _routeId, stop.Comment);
                        
                        // Помечаем комментарий как отправленный
                        stop.IsCommentSent = true;
                        
                        await Toast.Make(AppResources.ConfirmRoute_SendComment + " ✓", CommunityToolkit.Maui.Core.ToastDuration.Short).Show();
                        
                        // Пересортируем список
                        await ReorderStopsAsync();
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "OK");
                }
            });
            
            CancelRejectCommand = new Command<RouteStop>(stop =>
            {
                if (stop == null) return;
                
                // Сбрасываем состояние отклонения
                stop.IsRejected = false;
                stop.IsCommentSent = false;
                stop.Comment = string.Empty;
            });

            _viewModel = new ConfirmRouteViewModel(clientsData);
            BindingContext = _viewModel;
            
            // Подписываемся на изменение IsLoading
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        
        private bool _isFirstAppearing = true;
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Подписываемся на обновления маршрута
            _hubService.OnRouteUpdated += HandleRouteUpdated;
            
            // Загружаем данные только при первом появлении страницы
            if (_isFirstAppearing)
            {
                _isFirstAppearing = false;
                
                // Даём UI время отрисоваться перед загрузкой
                await Task.Delay(50);
                await _viewModel.LoadStopsAsync();
            }
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
                if (route?.ClientsData != null && route.ClientsData.Count > 0)
                {
                    // Обновляем локальный список
                    _clientsData.Clear();
                    _clientsData.AddRange(route.ClientsData);
                    
                    // Создаём новый ViewModel с обновлёнными данными
                    _viewModel = new ConfirmRouteViewModel(_clientsData);
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    BindingContext = _viewModel;
                    
                    // Загружаем точки
                    await _viewModel.LoadStopsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfirmRoutePage] HandleRouteUpdated error: {ex.Message}");
            }
        }
        
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfirmRouteViewModel.IsLoading))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingIndicator.IsRunning = _viewModel.IsLoading;
                    LoadingIndicator.IsVisible = _viewModel.IsLoading;
                    MainContent.IsVisible = !_viewModel.IsLoading;
                });
            }
        }
        
        /// <summary>
        /// Пересортировывает список точек (обработанные вниз)
        /// </summary>
        private async Task ReorderStopsAsync()
        {
            await Task.Delay(300); // Небольшая задержка для визуального эффекта
            
            var sortedStops = _viewModel.Stops
                .OrderBy(s => s.IsConfirmed || s.IsRejected)
                .ThenBy(s => s.OriginalIndex)
                .ToList();
            
            // Пересоздаём коллекцию в правильном порядке
            _viewModel.Stops.Clear();
            foreach (var stop in sortedStops)
            {
                _viewModel.Stops.Add(stop);
            }
        }

        private async void OnFinishRouteClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await _apiService.EndRouteAsync();
                if (result)
                {
                    // Очищаем локальные статусы
                    await _pickUpStatusService.ClearRouteAsync(_routeId);
                    
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