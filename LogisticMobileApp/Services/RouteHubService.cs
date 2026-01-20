using Microsoft.AspNetCore.SignalR.Client;
using CommunityToolkit.Maui.Alerts;

namespace LogisticMobileApp.Services
{
    /// <summary>
    /// DTO для события обновления маршрута от сервера
    /// </summary>
    public class RouteUpdatedDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>
    /// Сервис для подключения к SignalR хабу маршрутов
    /// </summary>
    public class RouteHubService : IAsyncDisposable
    {
        private const string HubUrl = "https://esme-aspiratory-september.ngrok-free.dev/hubs/notifications";
        
        private HubConnection? _connection;
        private bool _isInitialized = false;

        /// <summary>
        /// Показывать ли тост автоматически при получении события (по умолчанию true)
        /// </summary>
        public bool ShowToastOnUpdate { get; set; } = true;

        /// <summary>
        /// Событие при обновлении маршрута (добавлена новая точка и т.д.)
        /// Подписывайтесь, если нужно выполнить дополнительные действия (например, обновить список)
        /// </summary>
        public event Action<RouteUpdatedDto>? OnRouteUpdated;

        /// <summary>
        /// Событие при изменении состояния подключения
        /// </summary>
        public event Action<HubConnectionState>? OnConnectionStateChanged;

        /// <summary>
        /// Текущее состояние подключения
        /// </summary>
        public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

        /// <summary>
        /// Инициализирует подключение с токеном авторизации
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized && _connection != null)
                return;

            var token = await SecureStorage.Default.GetAsync("access_token");

            _connection = new HubConnectionBuilder()
                .WithUrl(HubUrl, options =>
                {
                    // Добавляем токен для аутентификации
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    }
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // Подписываемся на событие RouteUpdated от сервера
            _connection.On<RouteUpdatedDto>("RouteUpdated", data =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Автоматически показываем тост с информацией о новой точке
                    if (ShowToastOnUpdate)
                    {
                        var message = $"Новая точка: {data.Name}\n{data.Address}";
                        await Toast.Make(message, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
                    }

                    // Вызываем событие для подписчиков (если нужны доп. действия)
                    OnRouteUpdated?.Invoke(data);
                });
            });

            // Отслеживаем переподключение
            _connection.Reconnecting += error =>
            {
                OnConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
                System.Diagnostics.Debug.WriteLine($"[SignalR] Reconnecting: {error?.Message}");
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
                System.Diagnostics.Debug.WriteLine($"[SignalR] Reconnected: {connectionId}");
                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
                System.Diagnostics.Debug.WriteLine($"[SignalR] Closed: {error?.Message}");
                return Task.CompletedTask;
            };

            _isInitialized = true;
        }

        /// <summary>
        /// Запускает подключение к хабу
        /// </summary>
        public async Task StartAsync()
        {
            if (_connection == null)
            {
                await InitializeAsync();
            }

            if (_connection!.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _connection.StartAsync();
                    OnConnectionStateChanged?.Invoke(HubConnectionState.Connected);
                    System.Diagnostics.Debug.WriteLine("[SignalR] Connected to RouteHub");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalR] Connection failed: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Останавливает подключение
        /// </summary>
        public async Task StopAsync()
        {
            if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync();
                OnConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
                System.Diagnostics.Debug.WriteLine("[SignalR] Disconnected from RouteHub");
            }
        }

        /// <summary>
        /// Переинициализирует подключение (например, после обновления токена)
        /// </summary>
        public async Task ReinitializeAsync()
        {
            await StopAsync();
            _isInitialized = false;
            _connection = null;
            await InitializeAsync();
            await StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
