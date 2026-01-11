using System.Globalization;
using System.Text.Json;

namespace LogisticMobileApp.Services
{
    /// <summary>
    /// Модель навигационного шага
    /// </summary>
    public class NavigationStep
    {
        public string ManeuverType { get; set; } = string.Empty;      // turn, depart, arrive, merge, fork, etc.
        public string ManeuverModifier { get; set; } = string.Empty;  // left, right, straight, slight left, etc.
        public double Distance { get; set; }                           // Расстояние в метрах
        public double Duration { get; set; }                           // Время в секундах
        public string StreetName { get; set; } = string.Empty;        // Название улицы
        public (double lat, double lon) Location { get; set; }         // Координаты манёвра

        /// <summary>
        /// Получает иконку направления поворота
        /// </summary>
        public string DirectionIcon => ManeuverModifier switch
        {
            "left" => "↰",
            "slight left" => "↖",
            "sharp left" => "⬅",
            "right" => "↱",
            "slight right" => "↗",
            "sharp right" => "➡",
            "straight" => "↑",
            "uturn" => "↩",
            _ => ManeuverType switch
            {
                "depart" => "🚗",
                "arrive" => "🏁",
                _ => "→"
            }
        };

        /// <summary>
        /// Получает локализованное описание манёвра
        /// </summary>
        public string GetDescription(string language = "ru")
        {
            var direction = ManeuverModifier switch
            {
                "left" => language switch
                {
                    "ru" => "налево",
                    "en" => "left",
                    _ => "w lewo"  // pl
                },
                "slight left" => language switch
                {
                    "ru" => "плавно налево",
                    "en" => "slight left",
                    _ => "łagodnie w lewo"
                },
                "sharp left" => language switch
                {
                    "ru" => "резко налево",
                    "en" => "sharp left",
                    _ => "ostro w lewo"
                },
                "right" => language switch
                {
                    "ru" => "направо",
                    "en" => "right",
                    _ => "w prawo"
                },
                "slight right" => language switch
                {
                    "ru" => "плавно направо",
                    "en" => "slight right",
                    _ => "łagodnie w prawo"
                },
                "sharp right" => language switch
                {
                    "ru" => "резко направо",
                    "en" => "sharp right",
                    _ => "ostro w prawo"
                },
                "straight" => language switch
                {
                    "ru" => "прямо",
                    "en" => "straight",
                    _ => "prosto"
                },
                "uturn" => language switch
                {
                    "ru" => "разворот",
                    "en" => "U-turn",
                    _ => "zawracanie"
                },
                _ => ""
            };

            var action = ManeuverType switch
            {
                "depart" => language switch
                {
                    "ru" => "Начало маршрута",
                    "en" => "Start route",
                    _ => "Początek trasy"
                },
                "arrive" => language switch
                {
                    "ru" => "Прибытие",
                    "en" => "Arrival",
                    _ => "Przyjazd"
                },
                "turn" => language switch
                {
                    "ru" => $"Поверните {direction}",
                    "en" => $"Turn {direction}",
                    _ => $"Skręć {direction}"
                },
                "new name" => language switch
                {
                    "ru" => "Продолжайте движение",
                    "en" => "Continue",
                    _ => "Kontynuuj"
                },
                "merge" => language switch
                {
                    "ru" => "Слияние",
                    "en" => "Merge",
                    _ => "Włącz się"
                },
                "fork" => language switch
                {
                    "ru" => $"Держитесь {direction}",
                    "en" => $"Keep {direction}",
                    _ => $"Trzymaj się {direction}"
                },
                "roundabout" => language switch
                {
                    "ru" => "Круговое движение",
                    "en" => "Roundabout",
                    _ => "Rondo"
                },
                _ => language switch
                {
                    "ru" => $"Продолжайте {direction}",
                    "en" => $"Continue {direction}",
                    _ => $"Kontynuuj {direction}"
                }
            };

            return action;
        }

        /// <summary>
        /// Форматирует расстояние для отображения
        /// </summary>
        public string GetFormattedDistance(string language = "ru")
        {
            var (meterUnit, kmUnit) = language switch
            {
                "ru" => ("м", "км"),
                "en" => ("m", "km"),
                _ => ("m", "km")  // pl
            };

            return Distance switch
            {
                < 1000 => $"{Distance:F0} {meterUnit}",
                _ => $"{Distance / 1000:F1} {kmUnit}"
            };
        }

        /// <summary>
        /// Форматирует расстояние для отображения (по умолчанию русский)
        /// </summary>
        public string FormattedDistance => GetFormattedDistance("ru");
    }

    /// <summary>
    /// Результат маршрутизации с навигационными шагами
    /// </summary>
    public class RouteResult
    {
        public List<(double lat, double lon)> RoutePoints { get; set; } = new();
        public List<NavigationStep> Steps { get; set; } = new();
        public double TotalDistance { get; set; }  // метры
        public double TotalDuration { get; set; }  // секунды
    }

    /// <summary>
    /// Сервис для получения маршрутов по дорогам через OSRM API
    /// </summary>
    public class RoutingService
    {
        private readonly HttpClient _httpClient;
        private const string OsrmBaseUrl = "https://router.project-osrm.org/route/v1/driving/";

        public string? LastError { get; private set; }
        public bool LastRequestSuccessful { get; private set; }
        public List<NavigationStep> LastNavigationSteps { get; private set; } = new();

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

                var url = $"{OsrmBaseUrl}{coordString}?overview=full&geometries=geojson&steps=true";

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

                var routeResult = ParseOsrmResponseFull(json);

                if (routeResult.RoutePoints.Count > 0)
                {
                    LastRequestSuccessful = true;
                    LastNavigationSteps = routeResult.Steps;
                    System.Diagnostics.Debug.WriteLine($"[RoutingService] Got {routeResult.RoutePoints.Count} route points, {routeResult.Steps.Count} steps");
                    return routeResult.RoutePoints;
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

        private RouteResult ParseOsrmResponseFull(string json)
        {
            var result = new RouteResult();

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

                // Получаем общую дистанцию и время
                if (firstRoute.TryGetProperty("distance", out var distance))
                    result.TotalDistance = distance.GetDouble();
                if (firstRoute.TryGetProperty("duration", out var duration))
                    result.TotalDuration = duration.GetDouble();

                // Получаем геометрию
                if (firstRoute.TryGetProperty("geometry", out var geometry) &&
                    geometry.TryGetProperty("coordinates", out var coordsArray))
                {
                var coordCount = coordsArray.GetArrayLength();
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Found {coordCount} coordinates in response");

                // Парсим координаты (OSRM возвращает [lon, lat])
                foreach (var coord in coordsArray.EnumerateArray())
                {
                    if (coord.GetArrayLength() >= 2)
                    {
                        var lon = coord[0].GetDouble();
                        var lat = coord[1].GetDouble();
                            result.RoutePoints.Add((lat, lon));
                        }
                    }
                }

                // Парсим навигационные шаги из legs
                if (firstRoute.TryGetProperty("legs", out var legs))
                {
                    foreach (var leg in legs.EnumerateArray())
                    {
                        if (leg.TryGetProperty("steps", out var steps))
                        {
                            foreach (var step in steps.EnumerateArray())
                            {
                                var navStep = ParseNavigationStep(step);
                                if (navStep != null)
                                {
                                    result.Steps.Add(navStep);
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[RoutingService] Parsed {result.RoutePoints.Count} points, {result.Steps.Count} steps");
            }
            catch (Exception ex)
            {
                LastError = $"Ошибка парсинга: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Parse error: {ex.Message}");
            }

            return result;
        }

        private NavigationStep? ParseNavigationStep(JsonElement step)
        {
            try
            {
                var navStep = new NavigationStep();

                // Получаем манёвр
                if (step.TryGetProperty("maneuver", out var maneuver))
                {
                    if (maneuver.TryGetProperty("type", out var type))
                        navStep.ManeuverType = type.GetString() ?? "";
                    
                    if (maneuver.TryGetProperty("modifier", out var modifier))
                        navStep.ManeuverModifier = modifier.GetString() ?? "";

                    // Получаем координаты манёвра
                    if (maneuver.TryGetProperty("location", out var location) && location.GetArrayLength() >= 2)
                    {
                        var lon = location[0].GetDouble();
                        var lat = location[1].GetDouble();
                        navStep.Location = (lat, lon);
                    }
                }

                // Расстояние и время
                if (step.TryGetProperty("distance", out var distance))
                    navStep.Distance = distance.GetDouble();
                
                if (step.TryGetProperty("duration", out var duration))
                    navStep.Duration = duration.GetDouble();

                // Название улицы
                if (step.TryGetProperty("name", out var name))
                    navStep.StreetName = name.GetString() ?? "";

                return navStep;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RoutingService] Step parse error: {ex.Message}");
                return null;
            }
        }
    }
}

