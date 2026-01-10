using System.Text.Json;

namespace LogisticMobileApp.Services
{
    /// <summary>
    /// Статус обработки точки
    /// </summary>
    public class PickUpStatus
    {
        public int ClientId { get; set; }
        public int RouteId { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsRejected { get; set; }
        public string? Comment { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Сервис для локального хранения статусов сбора мусора
    /// </summary>
    public class PickUpStatusService
    {
        private readonly string _filePath;
        private List<PickUpStatus> _statuses = new();
        private bool _isLoaded = false;

        public PickUpStatusService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "pickup_statuses.json");
        }

        /// <summary>
        /// Загружает статусы из файла (если ещё не загружены)
        /// </summary>
        private async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;

            try
            {
                if (File.Exists(_filePath))
                {
                    var json = await File.ReadAllTextAsync(_filePath);
                    _statuses = JsonSerializer.Deserialize<List<PickUpStatus>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PickUpStatusService] Load error: {ex.Message}");
                _statuses = new();
            }

            _isLoaded = true;
        }

        /// <summary>
        /// Сохраняет статусы в файл
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_statuses, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PickUpStatusService] Save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Отмечает точку как подтверждённую
        /// </summary>
        public async Task ConfirmAsync(int clientId, int routeId)
        {
            await EnsureLoadedAsync();

            // Удаляем старый статус если есть
            _statuses.RemoveAll(s => s.ClientId == clientId && s.RouteId == routeId);

            _statuses.Add(new PickUpStatus
            {
                ClientId = clientId,
                RouteId = routeId,
                IsConfirmed = true,
                IsRejected = false,
                ProcessedAt = DateTime.Now
            });

            await SaveAsync();
        }

        /// <summary>
        /// Отмечает точку как отклонённую
        /// </summary>
        public async Task RejectAsync(int clientId, int routeId, string? comment = null)
        {
            await EnsureLoadedAsync();

            // Удаляем старый статус если есть
            _statuses.RemoveAll(s => s.ClientId == clientId && s.RouteId == routeId);

            _statuses.Add(new PickUpStatus
            {
                ClientId = clientId,
                RouteId = routeId,
                IsConfirmed = false,
                IsRejected = true,
                Comment = comment,
                ProcessedAt = DateTime.Now
            });

            await SaveAsync();
        }

        /// <summary>
        /// Получает статус точки
        /// </summary>
        public async Task<PickUpStatus?> GetStatusAsync(int clientId, int routeId)
        {
            await EnsureLoadedAsync();
            return _statuses.FirstOrDefault(s => s.ClientId == clientId && s.RouteId == routeId);
        }

        /// <summary>
        /// Проверяет, обработана ли точка (подтверждена или отклонена)
        /// </summary>
        public async Task<bool> IsProcessedAsync(int clientId, int routeId)
        {
            var status = await GetStatusAsync(clientId, routeId);
            return status != null;
        }

        /// <summary>
        /// Получает все статусы для маршрута
        /// </summary>
        public async Task<List<PickUpStatus>> GetRouteStatusesAsync(int routeId)
        {
            await EnsureLoadedAsync();
            return _statuses.Where(s => s.RouteId == routeId).ToList();
        }

        /// <summary>
        /// Получает ID всех обработанных точек для маршрута
        /// </summary>
        public async Task<HashSet<int>> GetProcessedClientIdsAsync(int routeId)
        {
            await EnsureLoadedAsync();
            return _statuses
                .Where(s => s.RouteId == routeId)
                .Select(s => s.ClientId)
                .ToHashSet();
        }

        /// <summary>
        /// Очищает статусы для маршрута (при завершении маршрута)
        /// </summary>
        public async Task ClearRouteAsync(int routeId)
        {
            await EnsureLoadedAsync();
            _statuses.RemoveAll(s => s.RouteId == routeId);
            await SaveAsync();
        }

        /// <summary>
        /// Очищает все статусы
        /// </summary>
        public async Task ClearAllAsync()
        {
            _statuses.Clear();
            _isLoaded = true;
            await SaveAsync();
        }
    }
}
