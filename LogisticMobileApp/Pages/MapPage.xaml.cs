using LogisticMobileApp.Models;
using LogisticMobileApp.Resources.Strings;
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
    public class RoutePointItem : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isConfirmed;
        private bool _isRejected;

        public int ClientId { get; set; }
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public MPoint? MapPoint { get; set; }
        
        public bool IsConfirmed
        {
            get => _isConfirmed;
            set { _isConfirmed = value; OnPropertyChanged(nameof(IsConfirmed)); OnPropertyChanged(nameof(StatusColor)); }
        }
        
        public bool IsRejected
        {
            get => _isRejected;
            set { _isRejected = value; OnPropertyChanged(nameof(IsRejected)); OnPropertyChanged(nameof(StatusColor)); }
        }
        
        public bool IsProcessed => IsConfirmed || IsRejected;
        
        public Microsoft.Maui.Graphics.Color StatusColor
        {
            get
            {
                if (IsConfirmed) return Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50");
                if (IsRejected) return Microsoft.Maui.Graphics.Color.FromArgb("#D32F2F");
                return Microsoft.Maui.Graphics.Color.FromArgb("#D32F2F"); // Default red
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    public partial class MapPage : ContentPage
    {
        private readonly List<ClientData> _clientsData;
        private readonly string? _geometryJson;
        private readonly RoutingService _routingService;
        private readonly ApiService _apiService;
        private readonly PickUpStatusService _pickUpStatusService;
        private readonly int _routeId;
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
        
        // Navigation mode
        private bool _isNavigationMode = false;
        private bool _isTurnsListExpanded = false;
        private List<NavigationStep> _navigationSteps = new();

        public MapPage(List<ClientData> clientsData, string? geometryJson = null, ApiService? apiService = null)
        {
            InitializeComponent();
            _clientsData = clientsData ?? new List<ClientData>();
            _geometryJson = geometryJson;
            _routingService = new RoutingService();
            _apiService = apiService ?? App.Services.GetRequiredService<ApiService>();
            _pickUpStatusService = App.Services.GetRequiredService<PickUpStatusService>();
            _routeId = Preferences.Get("RouteId", 0);
            
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
            SwipeHintLabel.Text = AppResources.Map_TapToExpand;
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
                        SwipeHintLabel.Text = AppResources.Map_TapToCollapse;
                    }
                    else
                    {
                        PointsCollectionView.IsVisible = false;
                        PointsCollectionView.HeightRequest = 0;
                        SwipeHintLabel.Text = AppResources.Map_TapToExpand;
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
            SwipeHintLabel.Text = AppResources.Map_TapToCollapse;
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
            SwipeHintLabel.Text = AppResources.Map_TapToExpand;
            
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
                MapControl.Map.Navigator.ZoomTo(10);
                
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

                // Переупорядочиваем точки: необработанные сначала, обработанные в конце
                await ReorderClientsByStatusAsync();

                // Заполняем список точек для bottom sheet
                await PopulateRoutePointsListAsync();

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

        /// <summary>
        /// Переупорядочивает клиентов и маркеры: необработанные сначала, обработанные в конце
        /// </summary>
        private async Task ReorderClientsByStatusAsync()
        {
            if (_clientsData.Count == 0) return;

            // Получаем все обработанные точки
            var processedIds = await _pickUpStatusService.GetProcessedClientIdsAsync(_routeId);
            
            if (processedIds.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[MapPage] No processed points found, keeping original order");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[MapPage] Found {processedIds.Count} processed points, reordering...");

            // Создаём списки для необработанных и обработанных
            var unprocessedClients = new List<ClientData>();
            var unprocessedMarkers = new List<MPoint>();
            var processedClients = new List<ClientData>();
            var processedMarkers = new List<MPoint>();

            for (int i = 0; i < _clientsData.Count; i++)
            {
                var client = _clientsData[i];
                var marker = i < _markerPoints.Count ? _markerPoints[i] : null;

                if (processedIds.Contains(client.Id))
                {
                    processedClients.Add(client);
                    if (marker != null) processedMarkers.Add(marker);
                }
                else
                {
                    unprocessedClients.Add(client);
                    if (marker != null) unprocessedMarkers.Add(marker);
                }
            }

            // Очищаем и заполняем заново: сначала необработанные, потом обработанные
            _clientsData.Clear();
            _clientsData.AddRange(unprocessedClients);
            _clientsData.AddRange(processedClients);

            _markerPoints.Clear();
            _markerPoints.AddRange(unprocessedMarkers);
            _markerPoints.AddRange(processedMarkers);

            System.Diagnostics.Debug.WriteLine($"[MapPage] Reordered: {unprocessedClients.Count} unprocessed first, {processedClients.Count} processed last");
            
            if (_clientsData.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] First client after reorder: {_clientsData[0].Name}, ID: {_clientsData[0].Id}");
            }
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
            // ВСЕГДА берём координаты первого клиента из _clientsData (он может меняться!)
            (double lat, double lon)? firstPoint = null;

            if (_clientsData.Count > 0)
            {
                // Берём координаты первого клиента
                firstPoint = ParseCoordinates(_clientsData[0].Coordinates);
                System.Diagnostics.Debug.WriteLine($"[MapPage] Target client: {_clientsData[0].Name}, ID: {_clientsData[0].Id}");
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
                // Если не удалось построить маршрут, добавляем прямую линию к точке
                if (_myLocationPoint != null)
                {
                    _routeFromMyLocation.Add(_myLocationPoint);
                    // Также добавляем точку назначения для отображения линии
                    if (_markerPoints.Count > 0)
                    {
                        _routeFromMyLocation.Add(_markerPoints[0]);
                    }
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
                    MapControl.Map.Navigator.ZoomTo(10);
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

        private async void OnNavigateClicked(object sender, EventArgs e)
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                // Обновляем текущее местоположение
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Разрешение", "Для навигации необходимо разрешение на геолокацию", "OK");
                        return;
                    }
                }

                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.High,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    _myLocationCoords = (location.Latitude, location.Longitude);
                    var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                    _myLocationPoint = new MPoint(x, y);

                    // Перестраиваем маршрут от моего местоположения до первой точки
                    await BuildRouteFromMyLocationAsync();

                    // Сохраняем навигационные шаги
                    _navigationSteps = _routingService.LastNavigationSteps;

                    // Показываем только маршрут от меня до первой точки
                    UpdateMapLayersNavigationOnly();

                    // Центрируем карту на моем местоположении
                    MapControl.Map.Navigator.CenterOn(_myLocationPoint);
                    MapControl.Map.Navigator.ZoomTo(5);

                    // Включаем режим навигации
                    await EnableNavigationModeAsync();
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
                await DisplayAlert("Ошибка", $"Ошибка навигации: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task EnableNavigationModeAsync()
        {
            _isNavigationMode = true;
            
            // Скрываем обычные кнопки
            NormalButtonsPanel.IsVisible = false;
            
            // Показываем панель навигации
            NavigationPanel.IsVisible = true;
            
            // Увеличиваем высоту BottomSheet для навигации
            _bottomSheetMinHeight = 190;
            BottomSheet.HeightRequest = _bottomSheetMinHeight;
            _bottomSheetCurrentHeight = _bottomSheetMinHeight;
            
            // Убираем первую точку из списка (она отображается в панели навигации)
            await UpdateRoutePointsListForNavigationAsync();
            
            // Заполняем информацию о точке назначения
            UpdateDestinationInfo();
            
            // Обновляем информацию о навигации
            UpdateNavigationInfo();
        }

        private async void DisableNavigationMode()
        {
            _isNavigationMode = false;
            _isTurnsListExpanded = false;
            
            // Показываем обычные кнопки
            NormalButtonsPanel.IsVisible = true;
            
            // Скрываем панель навигации и список поворотов
            NavigationPanel.IsVisible = false;
            TurnsListPanel.IsVisible = false;
            TurnsExpandIcon.Text = "▲";
            
            // Восстанавливаем стандартную высоту BottomSheet
            _bottomSheetMinHeight = 130;
            BottomSheet.HeightRequest = _bottomSheetMinHeight;
            _bottomSheetCurrentHeight = _bottomSheetMinHeight;
            
            // Восстанавливаем полный список точек
            await PopulateRoutePointsListAsync();
            
            // Возвращаем полный маршрут
            UpdateMapLayers();
        }

        private async Task UpdateRoutePointsListForNavigationAsync()
        {
            _routePointItems.Clear();
            
            // Получаем статусы обработанных точек
            var processedStatuses = await _pickUpStatusService.GetRouteStatusesAsync(_routeId);
            var statusDict = processedStatuses.ToDictionary(s => s.ClientId);
            
            // Начинаем со второй точки (индекс 1), первая уже отображается в панели навигации
            // Сначала добавляем необработанные точки
            for (int i = 1; i < _clientsData.Count; i++)
            {
                var client = _clientsData[i];
                if (!statusDict.ContainsKey(client.Id))
                {
                    MPoint? mapPoint = i < _markerPoints.Count ? _markerPoints[i] : null;
                    _routePointItems.Add(new RoutePointItem
                    {
                        ClientId = client.Id,
                        Index = _routePointItems.Count + 2, // +2 потому что первая точка на панели
                        Name = client.Name,
                        Address = client.Address,
                        MapPoint = mapPoint,
                        IsConfirmed = false,
                        IsRejected = false
                    });
                }
            }
            
            // Затем добавляем обработанные точки (в конец списка)
            for (int i = 1; i < _clientsData.Count; i++)
            {
                var client = _clientsData[i];
                if (statusDict.TryGetValue(client.Id, out var status))
                {
                    MPoint? mapPoint = i < _markerPoints.Count ? _markerPoints[i] : null;
                    _routePointItems.Add(new RoutePointItem
                    {
                        ClientId = client.Id,
                        Index = _routePointItems.Count + 2,
                        Name = client.Name,
                        Address = client.Address,
                        MapPoint = mapPoint,
                        IsConfirmed = status.IsConfirmed,
                        IsRejected = status.IsRejected
                    });
                }
            }
        }

        private void UpdateDestinationInfo()
        {
            if (_clientsData.Count > 0)
            {
                var firstClient = _clientsData[0];
                DestinationNameLabel.Text = firstClient.Name ?? "";
                DestinationAddressLabel.Text = firstClient.Address ?? "";
            }
            else
            {
                DestinationNameLabel.Text = "";
                DestinationAddressLabel.Text = "";
            }
        }

        private async void OnConfirmPickUpClicked(object sender, EventArgs e)
        {
            if (_clientsData.Count == 0)
                return;

            var client = _clientsData[0];
            
            // Показываем уведомление
            await CommunityToolkit.Maui.Alerts.Toast
                .Make($"✓ {client.Name}", CommunityToolkit.Maui.Core.ToastDuration.Short)
                .Show();
            
            // Обрабатываем точку и переходим к следующей
            await ProcessPointAndMoveToNext(isConfirmed: true);
        }

        private async void OnDeniedPickUpClicked(object sender, EventArgs e)
        {
            if (_clientsData.Count == 0)
                return;

            var client = _clientsData[0];
            
            // Показываем диалог для ввода причины отказа
            var comment = await DisplayPromptAsync(
                AppResources.ConfirmRoute_RejectButton,
                $"{client.Name}\n{client.Address}",
                AppResources.ConfirmRoute_SendComment,
                AppResources.Language_Cancel,
                AppResources.ConfirmRoute_CommentPlaceholder,
                maxLength: 500,
                keyboard: Keyboard.Text);

            if (string.IsNullOrWhiteSpace(comment))
                return;

            try
            {
                // Отправляем на сервер
                await _apiService.AddNoteAsync(client.Id, comment);
                
                // Показываем уведомление
                await CommunityToolkit.Maui.Alerts.Toast
                    .Make($"✗ {client.Name}", CommunityToolkit.Maui.Core.ToastDuration.Short)
                    .Show();
                
                // Обрабатываем точку и переходим к следующей
                await ProcessPointAndMoveToNext(isConfirmed: false, comment);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void OnBackFromNavigationClicked(object sender, EventArgs e)
        {
            DisableNavigationMode();
            CenterMapOnRoute();
        }

        private void UpdateNavigationInfo()
        {
            // Получаем текущий язык
            var lang = Preferences.Get("Language", System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            
            if (_navigationSteps.Count > 0)
            {
                // Находим следующий актуальный шаг (пропускаем depart если есть следующий)
                var stepIndex = 0;
                if (_navigationSteps.Count > 1 && _navigationSteps[0].ManeuverType == "depart")
                {
                    stepIndex = 1;
                }
                
                var nextStep = _navigationSteps[stepIndex];
                
                // Обновляем UI
                NavigationDirectionIcon.Text = nextStep.DirectionIcon;
                NavigationInstructionLabel.Text = nextStep.GetDescription(lang);
                NavigationDistanceLabel.Text = nextStep.GetFormattedDistance(lang);
                NavigationStreetLabel.Text = string.IsNullOrEmpty(nextStep.StreetName) ? "" : $"• {nextStep.StreetName}";
            }
            else
            {
                // Нет шагов навигации
                NavigationDirectionIcon.Text = "🏁";
                NavigationInstructionLabel.Text = lang switch
                {
                    "ru" => "Следуйте по маршруту",
                    "en" => "Follow the route",
                    _ => "Podążaj trasą"  // pl
                };
                NavigationDistanceLabel.Text = "";
                NavigationStreetLabel.Text = "";
            }
        }

        private void OnNavigationInfoTapped(object? sender, TappedEventArgs e)
        {
            _isTurnsListExpanded = !_isTurnsListExpanded;
            
            if (_isTurnsListExpanded)
            {
                PopulateTurnsList();
                TurnsListPanel.IsVisible = true;
                TurnsExpandIcon.Text = "▼";
            }
            else
            {
                TurnsListPanel.IsVisible = false;
                TurnsExpandIcon.Text = "▲";
            }
        }

        private void PopulateTurnsList()
        {
            TurnsListContainer.Children.Clear();
            
            if (_navigationSteps.Count == 0)
                return;
            
            var lang = Preferences.Get("Language", System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            
            // Создаём список поворотов в обратном порядке (снизу вверх)
            var reversedSteps = _navigationSteps.AsEnumerable().Reverse().ToList();
            
            foreach (var step in reversedSteps)
            {
                // Пропускаем depart и arrive если нужно
                if (step.ManeuverType == "depart" && _navigationSteps.Count > 1)
                    continue;
                
                var turnItem = CreateTurnListItem(step, lang);
                TurnsListContainer.Children.Add(turnItem);
            }
        }

        private View CreateTurnListItem(NavigationStep step, string lang)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(40)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                Padding = new Thickness(8, 6)
            };
            
            // Иконка направления
            var iconBorder = new Border
            {
                WidthRequest = 32,
                HeightRequest = 32,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                StrokeThickness = 0,
                BackgroundColor = step.ManeuverType == "arrive" 
                    ? Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50") 
                    : Microsoft.Maui.Graphics.Color.FromArgb("#FF5722"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = step.DirectionIcon,
                    TextColor = Colors.White,
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);
            
            // Инструкция и улица
            var textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Spacing = 1
            };
            
            textStack.Children.Add(new Label
            {
                Text = step.GetDescription(lang),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                    ? Colors.White 
                    : Microsoft.Maui.Graphics.Color.FromArgb("#212121"),
                LineBreakMode = LineBreakMode.TailTruncation
            });
            
            if (!string.IsNullOrEmpty(step.StreetName))
            {
                textStack.Children.Add(new Label
                {
                    Text = step.StreetName,
                    FontSize = 11,
                    TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                        ? Microsoft.Maui.Graphics.Color.FromArgb("#AAAAAA") 
                        : Microsoft.Maui.Graphics.Color.FromArgb("#757575"),
                    LineBreakMode = LineBreakMode.TailTruncation
                });
            }
            
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);
            
            // Расстояние
            var distanceLabel = new Label
            {
                Text = step.GetFormattedDistance(lang),
                FontSize = 12,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#FF5722"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(distanceLabel, 2);
            grid.Children.Add(distanceLabel);
            
            return grid;
        }

        private void UpdateMapLayersNavigationOnly()
        {
            if (_map == null) return;

            System.Diagnostics.Debug.WriteLine($"[MapPage] UpdateMapLayersNavigationOnly called");
            System.Diagnostics.Debug.WriteLine($"[MapPage] _routeFromMyLocation.Count: {_routeFromMyLocation.Count}");
            System.Diagnostics.Debug.WriteLine($"[MapPage] _markerPoints.Count: {_markerPoints.Count}");
            if (_markerPoints.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[MapPage] First marker point: {_markerPoints[0].X}, {_markerPoints[0].Y}");
            }

            // Удаляем ВСЕ слои кроме тайлов (MemoryLayer - это наши слои, тайлы - другой тип)
            var layersToRemove = _map.Layers.Where(l => l is MemoryLayer).ToList();
            System.Diagnostics.Debug.WriteLine($"[MapPage] Removing {layersToRemove.Count} layers");
            foreach (var layer in layersToRemove)
            {
                _map.Layers.Remove(layer);
            }

            // Добавляем только маршрут от моего местоположения до первой точки
            var navigationRouteLayer = CreateNavigationRouteLayer();
            if (navigationRouteLayer != null)
            {
                _map.Layers.Add(navigationRouteLayer);
                System.Diagnostics.Debug.WriteLine("[MapPage] Added navigation route layer");
            }

            // Добавляем маркер первой точки
            var firstPointLayer = CreateFirstPointMarkerLayer();
            if (firstPointLayer != null)
            {
                _map.Layers.Add(firstPointLayer);
                System.Diagnostics.Debug.WriteLine("[MapPage] Added first point marker layer");
            }

            // Добавляем маркер моего местоположения
            var myLocationLayer = CreateMyLocationLayer();
            if (myLocationLayer != null)
            {
                _map.Layers.Add(myLocationLayer);
                System.Diagnostics.Debug.WriteLine("[MapPage] Added my location layer");
            }

            // Принудительно обновляем карту
            _map.RefreshData();
            MapControl.Refresh();
            System.Diagnostics.Debug.WriteLine("[MapPage] Map refreshed");
        }

        private MemoryLayer? CreateNavigationRouteLayer()
        {
            if (_routeFromMyLocation.Count < 2 && _myLocationPoint == null)
                return null;

            var routePoints = new List<MPoint>();

            if (_routeFromMyLocation.Count >= 2)
            {
                routePoints.AddRange(_routeFromMyLocation);
            }
            else if (_myLocationPoint != null && _markerPoints.Count > 0)
            {
                // Fallback: прямая линия от меня до первой точки
                routePoints.Add(_myLocationPoint);
                routePoints.Add(_markerPoints[0]);
            }

            if (routePoints.Count < 2)
                return null;

            var coordinates = routePoints
                .Select(p => new Coordinate(p.X, p.Y))
                .ToArray();

            var lineString = new LineString(coordinates);
            var feature = new GeometryFeature(lineString)
            {
                Styles = new List<IStyle>
                {
                    new VectorStyle
                    {
                        Line = new Pen(Color.FromArgb(255, 255, 87, 34), 6) // Оранжевый цвет для навигации
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
                Name = "NavigationRoute",
                Features = new[] { feature },
                Style = null
            };
        }

        private MemoryLayer? CreateFirstPointMarkerLayer()
        {
            if (_markerPoints.Count == 0 || _clientsData.Count == 0)
                return null;

            var firstPoint = _markerPoints[0];
            var feature = new PointFeature(firstPoint)
            {
                Styles = new List<IStyle>
                {
                    // Зелёный круг для целевой точки
                    new SymbolStyle
                    {
                        SymbolScale = 1.4,
                        Fill = new Brush(Color.FromArgb(255, 76, 175, 80)), // Зелёный
                        Outline = new Pen(Color.White, 3),
                        SymbolType = SymbolType.Ellipse
                    },
                    // Номер внутри круга
                    new LabelStyle
                    {
                        Text = "1",
                        ForeColor = Color.White,
                        BackColor = new Brush(Color.Transparent),
                        Font = new Font { Size = 16, Bold = true },
                        HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                        VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                        Offset = new Offset(0, 0)
                    }
                }
            };

            return new MemoryLayer
            {
                Name = "FirstPointMarker",
                Features = new[] { feature },
                Style = null
            };
        }

        private void CenterMapOnNavigationRoute()
        {
            var allPoints = new List<MPoint>();

            if (_myLocationPoint != null)
                allPoints.Add(_myLocationPoint);

            if (_routeFromMyLocation.Count > 0)
                allPoints.AddRange(_routeFromMyLocation);
            else if (_markerPoints.Count > 0)
                allPoints.Add(_markerPoints[0]);

            if (allPoints.Count > 0)
            {
                CenterMapOnAllPoints(allPoints);
            }
        }

        private void UpdateMapLayers()
        {
            if (_map == null) return;

            // Удаляем ВСЕ слои кроме тайлов (MemoryLayer - это наши слои, тайлы - другой тип)
            var layersToRemove = _map.Layers.Where(l => l is MemoryLayer).ToList();
            foreach (var layer in layersToRemove)
            {
                _map.Layers.Remove(layer);
            }

            // Добавляем слой маршрута
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

            _map.RefreshData();
        }

        private async Task PopulateRoutePointsListAsync()
        {
            _routePointItems.Clear();
            
            // Получаем статусы обработанных точек
            var processedStatuses = await _pickUpStatusService.GetRouteStatusesAsync(_routeId);
            var statusDict = processedStatuses.ToDictionary(s => s.ClientId);
            
            // Сначала добавляем необработанные точки
            for (int i = 0; i < _clientsData.Count; i++)
            {
                var client = _clientsData[i];
                if (!statusDict.ContainsKey(client.Id))
                {
                    MPoint? mapPoint = i < _markerPoints.Count ? _markerPoints[i] : null;
                    _routePointItems.Add(new RoutePointItem
                    {
                        ClientId = client.Id,
                        Index = _routePointItems.Count + 1,
                        Name = client.Name,
                        Address = client.Address,
                        MapPoint = mapPoint,
                        IsConfirmed = false,
                        IsRejected = false
                    });
                }
            }
            
            // Затем добавляем обработанные точки (в конец списка)
            for (int i = 0; i < _clientsData.Count; i++)
            {
                var client = _clientsData[i];
                if (statusDict.TryGetValue(client.Id, out var status))
                {
                    MPoint? mapPoint = i < _markerPoints.Count ? _markerPoints[i] : null;
                    _routePointItems.Add(new RoutePointItem
                    {
                        ClientId = client.Id,
                        Index = _routePointItems.Count + 1,
                        Name = client.Name,
                        Address = client.Address,
                        MapPoint = mapPoint,
                        IsConfirmed = status.IsConfirmed,
                        IsRejected = status.IsRejected
                    });
                }
            }
        }

        private async Task ProcessPointAndMoveToNext(bool isConfirmed, string? comment = null)
        {
            if (_clientsData.Count == 0)
                return;

            var client = _clientsData[0];
            
            // Сохраняем статус локально
            if (isConfirmed)
            {
                await _pickUpStatusService.ConfirmAsync(client.Id, _routeId);
            }
            else
            {
                await _pickUpStatusService.RejectAsync(client.Id, _routeId, comment);
            }
            
            // Перемещаем точку в конец списка _clientsData
            _clientsData.RemoveAt(0);
            _clientsData.Add(client);
            
            // Перемещаем соответствующий маркер
            if (_markerPoints.Count > 0)
            {
                var markerPoint = _markerPoints[0];
                _markerPoints.RemoveAt(0);
                _markerPoints.Add(markerPoint);
            }
            
            // Проверяем, остались ли необработанные точки
            var processedIds = await _pickUpStatusService.GetProcessedClientIdsAsync(_routeId);
            var hasUnprocessedPoints = _clientsData.Any(c => !processedIds.Contains(c.Id));
            
            if (hasUnprocessedPoints)
            {
                // Находим первую необработанную точку
                var firstUnprocessedIndex = _clientsData.FindIndex(c => !processedIds.Contains(c.Id));
                
                if (firstUnprocessedIndex > 0)
                {
                    // Перемещаем необработанную точку в начало
                    var unprocessedClient = _clientsData[firstUnprocessedIndex];
                    _clientsData.RemoveAt(firstUnprocessedIndex);
                    _clientsData.Insert(0, unprocessedClient);
                    
                    if (firstUnprocessedIndex < _markerPoints.Count)
                    {
                        var unprocessedMarker = _markerPoints[firstUnprocessedIndex];
                        _markerPoints.RemoveAt(firstUnprocessedIndex);
                        _markerPoints.Insert(0, unprocessedMarker);
                    }
                }
                
                // Показываем индикатор загрузки
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;
                
                try
                {
                    // Получаем актуальное местоположение
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(5)
                    });
                    
                    if (location != null)
                    {
                        _myLocationCoords = (location.Latitude, location.Longitude);
                        var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                        _myLocationPoint = new MPoint(x, y);
                        System.Diagnostics.Debug.WriteLine($"[MapPage] New location: {location.Latitude}, {location.Longitude}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[MapPage] Next target client: {_clientsData[0].Name}, ID: {_clientsData[0].Id}");
                    System.Diagnostics.Debug.WriteLine($"[MapPage] MarkerPoints count: {_markerPoints.Count}");
                    
                    // Перестраиваем маршрут до следующей точки
                    await BuildRouteFromMyLocationAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[MapPage] Route rebuilt, points: {_routeFromMyLocation.Count}");
                    
                    // Получаем инструкции навигации для нового маршрута
                    await FetchNavigationSteps();
                    
                    // Обновляем UI
                    UpdateDestinationInfo();
                    UpdateNavigationInfo();
                    await UpdateRoutePointsListForNavigationAsync();
                    UpdateMapLayersNavigationOnly();
                    CenterMapOnNavigationRoute();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapPage] Error rebuilding route: {ex.Message}");
                }
                finally
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                }
            }
            else
            {
                // Все точки обработаны - выходим из режима навигации
                await DisplayAlert(
                    "✓",
                    Preferences.Get("Language", "ru") switch
                    {
                        "ru" => "Все точки маршрута обработаны!",
                        "en" => "All route points processed!",
                        _ => "Wszystkie punkty trasy przetworzone!"
                    },
                    "OK");
                
                DisableNavigationMode();
            }
        }

        private async Task FetchNavigationSteps()
        {
            if (_myLocationCoords == null || _clientsData.Count == 0)
            {
                _navigationSteps = new List<NavigationStep>();
                return;
            }

            var firstClient = _clientsData[0];
            var firstPoint = ParseCoordinates(firstClient.Coordinates);
            
            if (firstPoint == null)
            {
                _navigationSteps = new List<NavigationStep>();
                return;
            }

            var points = new List<(double lat, double lon)>
            {
                _myLocationCoords.Value,
                firstPoint.Value
            };

            // GetRouteAsync возвращает список точек маршрута
            // Навигационные шаги сохраняются в LastNavigationSteps
            await _routingService.GetRouteAsync(points);
            _navigationSteps = _routingService.LastNavigationSteps;
        }
    }
}
