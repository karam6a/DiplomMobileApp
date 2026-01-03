using System.Globalization;
using System.Text.Json;

namespace LogisticMobileApp.Services
{
    /// <summary>
    /// Сервис для получения маршрутов по дорогам через OSRM API
    /// </summary>
    public class RoutingService
    {
        private readonly HttpClient _httpClient;
        private const string OsrmBaseUrl = "https://router.project-osrm.org/route/v1/driving/";

        public string? LastError { get; private set; }
        public bool LastRequestSuccessful { get; private set; }

        public RoutingService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LogisticMobileApp/1.0 (MAUI Android)");
        }

        /// <summary>
        /// Получает маршрут между точками с учётом дорог
        /// </summary>
        /// <param name="coordinates">Список координат (lat, lon)</param>
        /// <returns>Список точек маршрута по дорогам (lat, lon)</returns>
        public async Task<List<(double lat, double lon)>> GetRouteAsync(List<(double lat, double lon)> coordinates)
        {
            LastError = null;
            LastRequestSuccessful = false;

            if (coordinates == null || coordinates.Count < 2)
            {
                LastError = "Недостаточно точек для построения маршрута";
                return new List<(double lat, double lon)>();
            }

            try
            {
                // Логируем входные координаты для отладки
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Input coordinates count: {coordinates.Count}");
                foreach (var (lat, lon) in coordinates)
                {
                    System.Diagnostics.Debug.WriteLine($"[RoutingService] Point: lat={lat}, lon={lon}");
                }

                // Проверяем валидность координат
                foreach (var (lat, lon) in coordinates)
                {
                    if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                    {
                        LastError = $"Невалидные координаты: lat={lat}, lon={lon}";
                        return coordinates;
                    }
                }

                // OSRM принимает координаты в формате lon,lat (не lat,lon!)
                var coordString = string.Join(";", coordinates.Select(c =>
                    $"{c.lon.ToString(CultureInfo.InvariantCulture)},{c.lat.ToString(CultureInfo.InvariantCulture)}"));

                var url = $"{OsrmBaseUrl}{coordString}?overview=full&geometries=geojson";

                System.Diagnostics.Debug.WriteLine($"[RoutingService] Requesting route: {url}");

                var response = await _httpClient.GetAsync(url);

                System.Diagnostics.Debug.WriteLine($"[RoutingService] Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[RoutingService] Error response: {errorBody}");
                    LastError = $"OSRM ошибка {response.StatusCode}. URL: {url.Substring(0, Math.Min(url.Length, 200))}...";
                    return coordinates;
                }

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Response length: {json.Length} chars");

                var routePoints = ParseOsrmResponse(json);

                if (routePoints.Count > 0)
                {
                    LastRequestSuccessful = true;
                    System.Diagnostics.Debug.WriteLine($"[RoutingService] Got {routePoints.Count} route points");
                    return routePoints;
                }
                else
                {
                    LastError = "OSRM не вернул точки маршрута";
                    return coordinates;
                }
            }
            catch (TaskCanceledException)
            {
                LastError = "Таймаут запроса к OSRM";
                return coordinates;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"Ошибка сети: {ex.Message}";
                return coordinates;
            }
            catch (Exception ex)
            {
                LastError = $"Ошибка: {ex.Message}";
                return coordinates;
            }
        }

        private List<(double lat, double lon)> ParseOsrmResponse(string json)
        {
            var result = new List<(double lat, double lon)>();

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Проверяем статус
                if (root.TryGetProperty("code", out var code))
                {
                    var codeStr = code.GetString();
                    System.Diagnostics.Debug.WriteLine($"[RoutingService] OSRM code: {codeStr}");
                    if (codeStr != "Ok")
                    {
                        LastError = $"OSRM код: {codeStr}";
                        return result;
                    }
                }

                // Получаем маршруты
                if (!root.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
                {
                    LastError = "OSRM не вернул маршруты";
                    System.Diagnostics.Debug.WriteLine("[RoutingService] No routes in response");
                    return result;
                }

                var firstRoute = routes[0];

                // Получаем геометрию
                if (!firstRoute.TryGetProperty("geometry", out var geometry))
                {
                    LastError = "Нет геометрии в ответе OSRM";
                    System.Diagnostics.Debug.WriteLine("[RoutingService] No geometry in route");
                    return result;
                }

                if (!geometry.TryGetProperty("coordinates", out var coordsArray))
                {
                    LastError = "Нет координат в геометрии OSRM";
                    System.Diagnostics.Debug.WriteLine("[RoutingService] No coordinates in geometry");
                    return result;
                }

                var coordCount = coordsArray.GetArrayLength();
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Found {coordCount} coordinates in response");

                // Парсим координаты (OSRM возвращает [lon, lat])
                foreach (var coord in coordsArray.EnumerateArray())
                {
                    if (coord.GetArrayLength() >= 2)
                    {
                        var lon = coord[0].GetDouble();
                        var lat = coord[1].GetDouble();
                        result.Add((lat, lon));
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[RoutingService] Parsed {result.Count} points");
            }
            catch (Exception ex)
            {
                LastError = $"Ошибка парсинга: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Parse error: {ex.Message}");
            }

            return result;
        }
    }
}

