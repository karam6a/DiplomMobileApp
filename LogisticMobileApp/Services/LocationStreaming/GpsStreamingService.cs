using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Channels;
using System.Diagnostics; // Для Debug.WriteLine
using TrackingService = logistics_server.Services.LocationStreaming.TrackingService;
using LocationMessage = logistics_server.Services.LocationStreaming.LocationMessage;
using TrackingResponse = logistics_server.Services.LocationStreaming.TrackingResponse;

namespace LogisticMobileApp.Services.LocationStreaming
{
    public class GpsStreamingService
    {
        private readonly IGpsListener _gpsListener;
        private const string ServerUrl = "https://esme-aspiratory-september.ngrok-free.dev";

        private Channel<LocationMessage> _locationChannel;
        private CancellationTokenSource _cts;
        private bool _isRunning = false;

        private string _currentDriverName = string.Empty;
        private string _currentLicensePlate = string.Empty;

        public GpsStreamingService(IGpsListener gpsListener)
        {
            _gpsListener = gpsListener;
        }

        public async Task StartTrackingAsync(string driverName, string licensePlate)
        {
            if (_isRunning) return;

            System.Diagnostics.Debug.WriteLine($"[Service DIAGNOSTIC] Зашел в StartTrackingAsync. Имя: {driverName}");

            _currentDriverName = driverName ?? "Unknown Driver";
            _currentLicensePlate = licensePlate ?? "Unknown Plate";
            _isRunning = true;

            // 1. Создаем канал (Это можно делать в фоне)
            _locationChannel = Channel.CreateBounded<LocationMessage>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _cts = new CancellationTokenSource();

            // 2. ЗАПУСК GPS (СТРОГО В ГЛАВНОМ ПОТОКЕ)
            // Android требует Looper для GPS слушателей. В фоне его нет.
            // Поэтому переключаемся на MainThread только для подписки.
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Проверка прав тоже любит MainThread
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        System.Diagnostics.Debug.WriteLine("[Service ERROR] Нет прав! Выходим.");
                        _isRunning = false;
                        return;
                    }

                    // Самое важное исправление:
                    _gpsListener.LocationChanged += HandleLocationUpdate;
                    _gpsListener.StartListening();

                    System.Diagnostics.Debug.WriteLine($"[Service SUCCESS] GPS Listener успешно запущен (MainThread)!");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Service CRASH] Ошибка при старте GPS на MainThread: {ex}");
                _isRunning = false;
                return;
            }

            // Если мы вылетели из-за ошибки или прав
            if (!_isRunning) return;

            // 3. Запуск сетевого цикла (ЭТО ОСТАВЛЯЕМ В ФОНЕ)
            // Сеть в MainThread запускать нельзя, поэтому тут Task.Run
            _ = Task.Run(() => ProcessQueueAsync(_cts.Token));

            // 4. Принудительная первая точка (Тоже лучше через MainThread для доступа к Geolocation)
            _ = Task.Run(async () =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () => await ForceSendInitialLocation());
            });
        }

        public async Task StopTrackingAsync()
        {
            if (!_isRunning) return;
            _isRunning = false;

            try
            {
                _gpsListener.StopListening();
                _gpsListener.LocationChanged -= HandleLocationUpdate;

                _cts?.Cancel();
                // Небольшая задержка, чтобы циклы успели остановиться
                await Task.Delay(200);
                _cts?.Dispose();

                Debug.WriteLine("[Service] Stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Service] Stop error: {ex.Message}");
            }
        }

        // --- PRODUCER (GPS) ---
        private void HandleLocationUpdate(object sender, Location location)
        {
            // Лог получения координат (раскомментируйте для отладки, если нужно)
            // Debug.WriteLine($"[GPS Event] New Fix: {location.Latitude}, {location.Longitude}");

            var msg = new LocationMessage
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Speed = location.Speed ?? 0,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DriverName = _currentDriverName,
                LicensePlate = _currentLicensePlate
            };

            if (!_locationChannel.Writer.TryWrite(msg))
            {
                Debug.WriteLine("[Buffer] Буфер полон, старая точка отброшена");
            }
        }

        // --- CONSUMER (NETWORK) ---
        private async Task ProcessQueueAsync(CancellationToken token)
        {
            Debug.WriteLine("[Loop] Starting send loop...");

            // Чтобы на схеме было понятно, как данные перетекают


            while (!token.IsCancellationRequested)
            {
                GrpcChannel channel = null;
                AsyncClientStreamingCall<LocationMessage, TrackingResponse> call = null;

                try
                {
                    var innerHandler = new SocketsHttpHandler
                    {
                        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                        EnableMultipleHttp2Connections = true
                    };

                    // Для Android эмулятора иногда нужен обход SSL
                    innerHandler.SslOptions.RemoteCertificateValidationCallback = (s, c, ch, e) => true;

                    var grpcWebHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, innerHandler);

                    channel = GrpcChannel.ForAddress(ServerUrl, new GrpcChannelOptions { HttpHandler = grpcWebHandler });
                    var client = new TrackingService.TrackingServiceClient(channel);

                    var deviceId = Preferences.Get("device_id", Guid.NewGuid().ToString());
                    var headers = new Metadata { { "x-device-id", deviceId } };

                    Debug.WriteLine("[Loop] Connecting to server...");
                    call = client.SendCoordinates(headers, cancellationToken: token);
                    Debug.WriteLine("[Loop] Connected!");

                    // Читаем из канала
                    while (await _locationChannel.Reader.WaitToReadAsync(token))
                    {
                        while (_locationChannel.Reader.TryRead(out var msg))
                        {
                            await call.RequestStream.WriteAsync(msg);
                            Debug.WriteLine($"[Network] >>> Sent: {msg.Latitude}, {msg.Longitude}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Loop] Error (Disconnect): {ex.Message}");
                    // Ждем перед реконнектом
                    await Task.Delay(5000, token);
                }
                finally
                {
                    if (call != null) try { await call.RequestStream.CompleteAsync(); call.Dispose(); } catch { }
                    if (channel != null) try { channel.Dispose(); } catch { }
                }
            }
        }

        private async Task ForceSendInitialLocation()
        {
            try
            {
                var location = await Geolocation.Default.GetLastKnownLocationAsync();

                // Если LastKnown нет, пробуем получить текущую (с тайм-аутом)
                if (location == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3));
                    location = await Geolocation.Default.GetLocationAsync(request);
                }

                if (location != null)
                {
                    HandleLocationUpdate(this, location);
                    Debug.WriteLine("[GPS] Initial location queued.");
                }
                else
                {
                    Debug.WriteLine("[GPS] No initial location available.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GPS] Initial fetch warning: {ex.Message}");
            }
        }
    }
}