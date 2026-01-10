using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;
using System.Collections.ObjectModel;
using System.Text.Json;
using Color = Mapsui.Styles.Color;
using Brush = Mapsui.Styles.Brush;
using Font = Mapsui.Styles.Font;

namespace LogisticMobileApp.Pages
{
    // Модель для отображения точки в списке
    public class RoutePointItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public MPoint? MapPoint { get; set; }
    }

    public partial class MapPage : ContentPage
    {
        private readonly List<ClientData> _clientsData;
        private readonly string? _geometryJson;
        private readonly RoutingService _routingService;
        private readonly List<MPoint> _markerPoints = new();
        private readonly List<MPoint> _routeLinePoints = new();
        private readonly List<MPoint> _routeFromMyLocation = new();
        private readonly ObservableCollection<RoutePointItem> _routePointItems = new();
        private MPoint? _myLocationPoint;
        private (double lat, double lon)? _myLocationCoords;
        private Mapsui.Map? _map;
        private bool _isMapInitialized = false;
        
        // Bottom Sheet
        private double _bottomSheetMinHeight = 130;
        private double _bottomSheetMaxHeight = 450;
        private double _bottomSheetCurrentHeight;
        private bool _isBottomSheetExpanded = false;

        public MapPage(List<ClientData> clientsData, string? geometryJson = null)
        {
            InitializeComponent();
            _clientsData = clientsData ?? new List<ClientData>();
            _geometryJson = geometryJson;
            _routingService = new RoutingService();
            
            // Привязываем коллекцию к CollectionView
            PointsCollectionView.ItemsSource = _routePointItems;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            if (!_isMapInitialized)
            {
                _isMapInitialized = true;
                
                // Инициализируем bottom sheet
                InitializeBottomSheet();
                
                Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(100);
                    await InitializeMapAsync();
                });
            }
        }

        private void InitializeBottomSheet()
        {
            _bottomSheetCurrentHeight = _bottomSheetMinHeight;
            BottomSheet.HeightRequest = _bottomSheetMinHeight;
            PointsCollectionView.HeightRequest = 0;
            PointsCollectionView.IsVisible = false;
            SwipeHintLabel.Text = "↑ Нажмите для списка точек";
        }

        private void OnBottomSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Вычисляем новую высоту
                    var newHeight = _bottomSheetCurrentHeight - e.TotalY;
                    newHeight = Math.Clamp(newHeight, _bottomSheetMinHeight, _bottomSheetMaxHeight);
                    BottomSheet.HeightRequest = newHeight;
                    
                    // Показываем/скрываем список в зависимости от высоты
                    var listHeight = newHeight - _bottomSheetMinHeight;
                    if (listHeight > 20)
                    {
                        PointsCollectionView.IsVisible = true;
                        PointsCollectionView.HeightRequest = listHeight;
                        SwipeHintLabel.Text = "↓ Нажмите для скрытия";
                    }
                    else
                    {
                        PointsCollectionView.IsVisible = false;
                        PointsCollectionView.HeightRequest = 0;
                        SwipeHintLabel.Text = "↑ Нажмите для списка точек";
                    }
                    break;

                case GestureStatus.Completed:
                    // Фиксируем позицию
                    _bottomSheetCurrentHeight = BottomSheet.HeightRequest;
                    
                    // Анимация до ближайшего состояния
                    if (_bottomSheetCurrentHeight > (_bottomSheetMinHeight + _bottomSheetMaxHeight) / 2)
                    {
                        ExpandBottomSheet();
                    }
                    else
                    {
                        CollapseBottomSheet();
                    }
                    break;
            }
        }

        private void OnDragHandleTapped(object? sender, TappedEventArgs e)
        {
            // Переключаем состояние bottom sheet по нажатию
            if (_isBottomSheetExpanded)
            {
                CollapseBottomSheet();
            }
            else
            {
                ExpandBottomSheet();
            }
        }

        private void ExpandBottomSheet()
        {
            _isBottomSheetExpanded = true;
            SwipeHintLabel.Text = "↓ Нажмите для скрытия";
            PointsCollectionView.IsVisible = true;
            
            var animation = new Animation(v => 
            {
                BottomSheet.HeightRequest = v;
                PointsCollectionView.HeightRequest = v - _bottomSheetMinHeight;
            }, BottomSheet.HeightRequest, _bottomSheetMaxHeight);
            
            animation.Commit(this, "ExpandSheet", 16, 250, Easing.CubicOut, (v, c) =>
            {
                _bottomSheetCurrentHeight = _bottomSheetMaxHeight;
            });
        }

        private void CollapseBottomSheet()
        {
            _isBottomSheetExpanded = false;
            SwipeHintLabel.Text = "↑ Нажмите для списка точек";
            
            var animation = new Animation(v => 
            {
                BottomSheet.HeightRequest = v;
                var listHeight = v - _bottomSheetMinHeight;
                PointsCollectionView.HeightRequest = Math.Max(0, listHeight);
            }, BottomSheet.HeightRequest, _bottomSheetMinHeight);
            
            animation.Commit(this, "CollapseSheet", 16, 250, Easing.CubicOut, (v, c) =>
            {
                _bottomSheetCurrentHeight = _bottomSheetMinHeight;
                PointsCollectionView.IsVisible = false;
                PointsCollectionView.HeightRequest = 0;
            });
        }

        private void PopulateRoutePointsList()
        {
            _routePointItems.Clear();
            
            for (int i = 0; i < _clientsData.Count; i++)
            {
                var client = _clientsData[i];
                MPoint? mapPoint = i < _markerPoints.Count ? _markerPoints[i] : null;
                
                _routePointItems.Add(new RoutePointItem
                {
                    Index = i + 1,
                    Name = client.Name,
                    Address = client.Address,
                    MapPoint = mapPoint
                });
            }
        }

        private void OnPointSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is RoutePointItem selectedPoint && selectedPoint.MapPoint != null)
            {
                // Центрируем карту на выбранной точке
                MapControl.Map.Navigator.CenterOn(selectedPoint.MapPoint);
                MapControl.Map.Navigator.ZoomTo(3000);
                
                // Сворачиваем bottom sheet
                CollapseBottomSheet();
                
                // Сбрасываем выделение
                PointsCollectionView.SelectedItem = null;
            }
        }

        private async Task InitializeMapAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                Logger.LogDelegate = null;

                _map = new Mapsui.Map
                {
                    CRS = "EPSG:3857",
                    BackColor = Color.FromArgb(255, 242, 239, 233)
                };

                while (_map.Widgets.TryDequeue(out _)) { }
                
                var tileLayer = OpenStreetMap.CreateTileLayer();
                _map.Layers.Add(tileLayer);

                // Получаем текущее местоположение
                await GetCurrentLocationAsync();

                // Парсим координаты клиентов для маркеров
                ParseClientCoordinates();

                // Заполняем список точек для bottom sheet
                PopulateRoutePointsList();

                // Парсим готовый маршрут из сервера (если есть)
                ParseGeometryJson();

                // Строим маршрут от моего местоположения до первой точки
                await BuildRouteFromMyLocationAsync();

                // Добавляем слой с маршрутом (от моего местоположения до клиентов)
                var routeLayer = CreateRouteLayer();
                if (routeLayer != null)
                {
                    _map.Layers.Add(routeLayer);
                }

                // Добавляем слой с маркерами клиентов
                var markersLayer = CreateMarkersLayer();
                if (markersLayer != null)
                {
                    _map.Layers.Add(markersLayer);
                }

                // Добавляем маркер моего местоположения (поверх всего)
                var myLocationLayer = CreateMyLocationLayer();
                if (myLocationLayer != null)
                {
                    _map.Layers.Add(myLocationLayer);
                }

                MapControl.Map = _map;
                SetInitialViewport();
                _map.RefreshData();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить карту: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("[MapPage] Location permission denied");
                        return;
                    }
                }

                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    _myLocationCoords = (location.Latitude, location.Longitude);
                    var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                    _myLocationPoint = new MPoint(x, y);
                    System.Diagnostics.Debug.WriteLine($"[MapPage] Got location: {location.Latitude}, {location.Longitude}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MapPage] Location is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] Location error: {ex.Message}");
            }
        }

        private void ParseClientCoordinates()
        {
            _markerPoints.Clear();
            
            foreach (var client in _clientsData)
            {
                var point = ParseCoordinates(client.Coordinates);
                if (point == null) continue;

                var (x, y) = SphericalMercator.FromLonLat(point.Value.lon, point.Value.lat);
                _markerPoints.Add(new MPoint(x, y));
            }

            System.Diagnostics.Debug.WriteLine($"[MapPage] Parsed {_markerPoints.Count} marker points from clients");
        }

        private void ParseGeometryJson()
        {
            _routeLinePoints.Clear();

            if (string.IsNullOrWhiteSpace(_geometryJson))
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] No geometry JSON provided");
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(_geometryJson);
                var root = document.RootElement;

                if (root.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "LineString" && root.TryGetProperty("coordinates", out var coordinates))
                    {
                        ParseLineStringCoordinates(coordinates);
                    }
                    else if (type == "MultiLineString" && root.TryGetProperty("coordinates", out var multiCoords))
                    {
                        foreach (var lineCoords in multiCoords.EnumerateArray())
                        {
                            ParseLineStringCoordinates(lineCoords);
                        }
                    }
                    else if (type == "FeatureCollection" && root.TryGetProperty("features", out var features))
                    {
                        foreach (var feature in features.EnumerateArray())
                        {
                            if (feature.TryGetProperty("geometry", out var geometry))
                            {
                                ParseGeometryElement(geometry);
                            }
                        }
                    }
                    else if (type == "Feature" && root.TryGetProperty("geometry", out var geometry))
                    {
                        ParseGeometryElement(geometry);
                    }
                }
                else if (root.TryGetProperty("coordinates", out var coords))
                {
                    ParseLineStringCoordinates(coords);
                }

                System.Diagnostics.Debug.WriteLine($"[MapPage] Parsed {_routeLinePoints.Count} route points from geometry");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] Error parsing geometry JSON: {ex.Message}");
            }
        }

        private void ParseGeometryElement(JsonElement geometry)
        {
            if (geometry.TryGetProperty("type", out var typeEl) && 
                geometry.TryGetProperty("coordinates", out var coords))
            {
                var type = typeEl.GetString();
                if (type == "LineString")
                {
                    ParseLineStringCoordinates(coords);
                }
                else if (type == "MultiLineString")
                {
                    foreach (var lineCoords in coords.EnumerateArray())
                    {
                        ParseLineStringCoordinates(lineCoords);
                    }
                }
            }
        }

        private void ParseLineStringCoordinates(JsonElement coordinates)
        {
            foreach (var coord in coordinates.EnumerateArray())
            {
                if (coord.GetArrayLength() >= 2)
                {
                    var lon = coord[0].GetDouble();
                    var lat = coord[1].GetDouble();
                    
                    var (x, y) = SphericalMercator.FromLonLat(lon, lat);
                    _routeLinePoints.Add(new MPoint(x, y));
                }
            }
        }

        private async Task BuildRouteFromMyLocationAsync()
        {
            _routeFromMyLocation.Clear();

            if (_myLocationCoords == null)
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] No location for routing");
                return;
            }

            // Определяем первую точку маршрута
            (double lat, double lon)? firstPoint = null;

            if (_routeLinePoints.Count > 0)
            {
                // Если есть маршрут с сервера, берём его первую точку
                var first = _routeLinePoints[0];
                var (lon, lat) = SphericalMercator.ToLonLat(first.X, first.Y);
                firstPoint = (lat, lon);
            }
            else if (_clientsData.Count > 0)
            {
                // Иначе берём координаты первого клиента
                firstPoint = ParseCoordinates(_clientsData[0].Coordinates);
            }

            if (firstPoint == null)
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] No first point for routing");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[MapPage] Building route from ({_myLocationCoords.Value.lat}, {_myLocationCoords.Value.lon}) to ({firstPoint.Value.lat}, {firstPoint.Value.lon})");

            // Запрашиваем маршрут от моего местоположения до первой точки
            var coordinates = new List<(double lat, double lon)>
            {
                _myLocationCoords.Value,
                firstPoint.Value
            };

            var routePoints = await _routingService.GetRouteAsync(coordinates);

            if (_routingService.LastRequestSuccessful && routePoints.Count > 2)
            {
                // Конвертируем в Spherical Mercator
                foreach (var (lat, lon) in routePoints)
                {
                    var (x, y) = SphericalMercator.FromLonLat(lon, lat);
                    _routeFromMyLocation.Add(new MPoint(x, y));
                }
                System.Diagnostics.Debug.WriteLine($"[MapPage] Got {_routeFromMyLocation.Count} points for route from my location");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] Routing failed: {_routingService.LastError}");
                // Если не удалось построить маршрут, просто добавляем прямую линию
                if (_myLocationPoint != null)
                {
                    _routeFromMyLocation.Add(_myLocationPoint);
                }
            }
        }

        private void SetInitialViewport()
        {
            // Собираем все точки для определения области просмотра
            var allPoints = new List<MPoint>();
            
            // Добавляем маршрут от моего местоположения
            if (_routeFromMyLocation.Count > 0)
                allPoints.AddRange(_routeFromMyLocation);
            else if (_myLocationPoint != null)
                allPoints.Add(_myLocationPoint);
            
            // Добавляем основной маршрут
            if (_routeLinePoints.Count > 0)
                allPoints.AddRange(_routeLinePoints);
            else
                allPoints.AddRange(_markerPoints);

            if (allPoints.Count > 0)
            {
                CenterMapOnAllPoints(allPoints);
            }
            else
            {
                var (defaultX, defaultY) = SphericalMercator.FromLonLat(27.5615, 53.9006);
                MapControl.Map.Navigator.CenterOn(new MPoint(defaultX, defaultY));
                MapControl.Map.Navigator.ZoomTo(10000);
            }
        }

        private MemoryLayer? CreateMyLocationLayer()
        {
            if (_myLocationPoint == null)
                return null;

            var feature = new PointFeature(_myLocationPoint)
            {
                Styles = new List<IStyle>
                {
                    // Синий маркер для моего местоположения
                    new SymbolStyle
                    {
                        SymbolScale = 1.2,
                        Fill = new Brush(Color.FromArgb(255, 33, 150, 243)), // Синий
                        Outline = new Pen(Color.White, 3),
                        SymbolType = SymbolType.Ellipse
                    },
                    // Метка "Я" внутри круга
                    new LabelStyle
                    {
                        Text = "Я",
                        ForeColor = Color.White,
                        BackColor = new Brush(Color.Transparent),
                        Font = new Font { Size = 14, Bold = true },
                        HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                        VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                        Offset = new Offset(0, 0)
                    }
                }
            };

            return new MemoryLayer
            {
                Name = "MyLocation",
                Features = new[] { feature },
                Style = null
            };
        }

        private MemoryLayer? CreateMarkersLayer()
        {
            if (_markerPoints.Count == 0)
                return null;

            var features = new List<IFeature>();
            
            for (int i = 0; i < _markerPoints.Count && i < _clientsData.Count; i++)
            {
                var sphericalPoint = _markerPoints[i];
                var client = _clientsData[i];

                var feature = new PointFeature(sphericalPoint)
                {
                    Styles = CreateMarkerStyles(i + 1)
                };

                features.Add(feature);
            }

            if (features.Count == 0)
                return null;

            return new MemoryLayer
            {
                Name = "Markers",
                Features = features,
                Style = null
            };
        }

        private MemoryLayer? CreateRouteLayer()
        {
            var routePoints = new List<MPoint>();

            // 1. Добавляем маршрут от моего местоположения до первой точки (по дорогам)
            if (_routeFromMyLocation.Count > 0)
            {
                routePoints.AddRange(_routeFromMyLocation);
            }
            else if (_myLocationPoint != null)
            {
                // Fallback: прямая линия
                routePoints.Add(_myLocationPoint);
            }

            // 2. Добавляем основной маршрут (с сервера или прямые линии)
            if (_routeLinePoints.Count >= 2)
            {
                // Маршрут с сервера - пропускаем первую точку если она уже есть
                var startIndex = routePoints.Count > 0 ? 0 : 0;
                routePoints.AddRange(_routeLinePoints);
            }
            else if (_markerPoints.Count > 0)
            {
                // Прямые линии между клиентами
                routePoints.AddRange(_markerPoints);
            }

            if (routePoints.Count < 2)
                return null;

            // Удаляем дубликаты подряд идущих точек
            var cleanedPoints = new List<MPoint> { routePoints[0] };
            for (int i = 1; i < routePoints.Count; i++)
            {
                var prev = cleanedPoints[^1];
                var curr = routePoints[i];
                // Проверяем, что точки не слишком близко друг к другу
                if (Math.Abs(prev.X - curr.X) > 1 || Math.Abs(prev.Y - curr.Y) > 1)
                {
                    cleanedPoints.Add(curr);
                }
            }

            if (cleanedPoints.Count < 2)
                return null;

            var coordinates = cleanedPoints
                .Select(p => new Coordinate(p.X, p.Y))
                .ToArray();

            var lineString = new LineString(coordinates);
            var feature = new GeometryFeature(lineString)
            {
                Styles = new List<IStyle>
                {
                    new VectorStyle
                    {
                        Line = new Pen(Color.FromArgb(255, 25, 118, 210), 5)
                        {
                            PenStyle = PenStyle.Solid,
                            PenStrokeCap = PenStrokeCap.Round,
                            StrokeJoin = StrokeJoin.Round
                        }
                    }
                }
            };

            return new MemoryLayer
            {
                Name = "Route",
                Features = new[] { feature },
                Style = null
            };
        }

        private static List<IStyle> CreateMarkerStyles(int index)
        {
            return new List<IStyle>
            {
                // Красный круг
                new SymbolStyle
                {
                    SymbolScale = 1.2,
                    Fill = new Brush(Color.FromArgb(255, 211, 47, 47)), // Красный
                    Outline = new Pen(Color.White, 2),
                    SymbolType = SymbolType.Ellipse
                },
                // Номер внутри круга
                new LabelStyle
                {
                    Text = index.ToString(),
                    ForeColor = Color.White,
                    BackColor = new Brush(Color.Transparent),
                    Font = new Font { Size = 14, Bold = true },
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                    Offset = new Offset(0, 0)
                }
            };
        }

        private (double lat, double lon)? ParseCoordinates(string? coordinates)
        {
            if (string.IsNullOrWhiteSpace(coordinates))
                return null;

            var parts = coordinates.Split(',');
            if (parts.Length < 2)
                return null;

            if (double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }

            return null;
        }

        private void CenterMapOnAllPoints(List<MPoint> allPoints)
        {
            if (allPoints.Count == 0)
                return;

            if (allPoints.Count == 1)
            {
                MapControl.Map.Navigator.CenterOn(allPoints[0]);
                MapControl.Map.Navigator.ZoomTo(5000);
                return;
            }

            var minX = allPoints.Min(p => p.X);
            var maxX = allPoints.Max(p => p.X);
            var minY = allPoints.Min(p => p.Y);
            var maxY = allPoints.Max(p => p.Y);

            var paddingX = Math.Max((maxX - minX) * 0.15, 500);
            var paddingY = Math.Max((maxY - minY) * 0.15, 500);

            var extent = new MRect(
                minX - paddingX,
                minY - paddingY,
                maxX + paddingX,
                maxY + paddingY
            );

            MapControl.Map.Navigator.ZoomToBox(extent);
        }

        private void CenterMapOnRoute()
        {
            var allPoints = new List<MPoint>();
            
            if (_routeFromMyLocation.Count > 0)
                allPoints.AddRange(_routeFromMyLocation);
            else if (_myLocationPoint != null)
                allPoints.Add(_myLocationPoint);
            
            if (_routeLinePoints.Count > 0)
                allPoints.AddRange(_routeLinePoints);
            else
                allPoints.AddRange(_markerPoints);

            CenterMapOnAllPoints(allPoints);
        }

        private void OnCenterRouteClicked(object sender, EventArgs e)
        {
            CenterMapOnRoute();
        }

        private async void OnMyLocationClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Разрешение", "Для определения местоположения необходимо разрешение", "OK");
                        return;
                    }
                }

                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    _myLocationCoords = (location.Latitude, location.Longitude);
                    var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                    _myLocationPoint = new MPoint(x, y);
                    
                    // Перестраиваем маршрут от нового местоположения
                    await BuildRouteFromMyLocationAsync();
                    
                    // Перестраиваем слои
                    UpdateMapLayers();

                    MapControl.Map.Navigator.CenterOn(_myLocationPoint);
                    MapControl.Map.Navigator.ZoomTo(2000);
                }
                else
                {
                    await DisplayAlert("Ошибка", "Не удалось определить местоположение", "OK");
                }
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("Ошибка", "Геолокация не поддерживается на этом устройстве", "OK");
            }
            catch (PermissionException)
            {
                await DisplayAlert("Ошибка", "Нет разрешения на геолокацию", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка геолокации: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void UpdateMapLayers()
        {
            if (_map == null) return;

            // Удаляем старые слои маршрута и моего местоположения
            var layersToRemove = _map.Layers.Where(l => l.Name == "Route" || l.Name == "MyLocation").ToList();
            foreach (var layer in layersToRemove)
            {
                _map.Layers.Remove(layer);
            }

            // Добавляем обновлённые слои
            var routeLayer = CreateRouteLayer();
            if (routeLayer != null)
            {
                _map.Layers.Add(routeLayer);
            }

            var myLocationLayer = CreateMyLocationLayer();
            if (myLocationLayer != null)
            {
                _map.Layers.Add(myLocationLayer);
            }

            _map.RefreshData();
        }
    }
}
