using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Channels; // !!! ВАЖНО: Добавить этот using
using TrackingService = logistics_server.Services.LocationStreaming.TrackingService;
using LocationMessage = logistics_server.Services.LocationStreaming.LocationMessage;
using TrackingResponse = logistics_server.Services.LocationStreaming.TrackingResponse;

namespace LogisticMobileApp.Services.LocationStreaming
{
    public class GpsStreamingService
    {
        private readonly IGpsListener _gpsListener;
        private const string ServerUrl = "https://esme-aspiratory-september.ngrok-free.dev"; // Проверьте протокол

        // Буфер для координат (бесконечный или ограниченный)
        private Channel<LocationMessage> _locationChannel;
        private CancellationTokenSource _cts;
        private bool _isRunning = false;

        public GpsStreamingService(IGpsListener gpsListener)
        {
            _gpsListener = gpsListener;
        }

        public async Task StartTrackingAsync()
        {
            if (_isRunning) return;
            _isRunning = true;

            // 1. Проверяем права (Permissions)
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    _isRunning = false;
                    return;
                }
            }

            // 2. Создаем канал (очередь)
            // Bounded(100) означает, что если интернета нет долго, мы сохраним последние 100 точек,
            // а старые начнем выбрасывать, чтобы не забить память.
            _locationChannel = Channel.CreateBounded<LocationMessage>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _cts = new CancellationTokenSource();

            // 3. Подписываемся на GPS (Производитель)
            _gpsListener.LocationChanged += HandleLocationUpdate;
            _gpsListener.StartListening();

            Console.WriteLine("[Service] GPS Listener started.");

            // 4. Запускаем фоновую задачу отправки (Потребитель)
            // Мы не ждем ее (no await), она крутится сама по себе
            _ = Task.Run(() => ProcessQueueAsync(_cts.Token));

            // 5. Принудительная отправка первой точки (как мы обсуждали)
            _ = Task.Run(async () => await ForceSendInitialLocation());
        }

        public async Task StopTrackingAsync()
        {
            if (!_isRunning) return;
            _isRunning = false;

            _gpsListener.StopListening();
            _gpsListener.LocationChanged -= HandleLocationUpdate;

            // Отменяем фоновую задачу отправки
            _cts?.Cancel();

            // Ждем немного, чтобы канал успел закрыться (опционально)
            await Task.Delay(100);
            _cts?.Dispose();

            Console.WriteLine("[Service] Stopped.");
        }

        // --- ПРОИЗВОДИТЕЛЬ (GPS Event) ---
        // Этот метод просто кидает данные в буфер. Он работает мгновенно и не зависит от сети.
        private void HandleLocationUpdate(object sender, Location location)
        {
            var msg = new LocationMessage
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Speed = location.Speed ?? 0,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // TryWrite вернет false, если буфер переполнен (мы настроили удалять старые),
            // так что это безопасно.
            _locationChannel.Writer.TryWrite(msg);
            // Console.WriteLine($"[Buffer] Added: {msg.Latitude}, {msg.Longitude}");
        }

        // --- ПОТРЕБИТЕЛЬ (Сетевой цикл) ---
        // Вся магия реконнекта здесь
        private async Task ProcessQueueAsync(CancellationToken token)
        {
            Console.WriteLine("[Loop] Starting send loop...");

            while (!token.IsCancellationRequested)
            {
                GrpcChannel channel = null;
                AsyncClientStreamingCall<LocationMessage, TrackingResponse> call = null;

                try
                {
                    // 1. Настройка соединения
                    var innerHandler = new SocketsHttpHandler
                    {
                        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                        EnableMultipleHttp2Connections = true
                    };
                    innerHandler.SslOptions.RemoteCertificateValidationCallback = (s, c, ch, e) => true;

                    var grpcWebHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, innerHandler);

                    // Создаем канал
                    channel = GrpcChannel.ForAddress(ServerUrl, new GrpcChannelOptions { HttpHandler = grpcWebHandler });
                    var client = new TrackingService.TrackingServiceClient(channel);

                    // Заголовки
                    var deviceId = Preferences.Get("device_id", Guid.NewGuid().ToString());
                    var headers = new Metadata { { "x-device-id", deviceId } };

                    Console.WriteLine("[Loop] Connecting to server...");
                    call = client.SendCoordinates(headers, cancellationToken: token);
                    Console.WriteLine("[Loop] Connected!");

                    // 2. Читаем из буфера и отправляем
                    // WaitToReadAsync будет ждать, пока в буфере что-то появится (не грузит процессор)
                    while (await _locationChannel.Reader.WaitToReadAsync(token))
                    {
                        while (_locationChannel.Reader.TryRead(out var msg))
                        {
                            await call.RequestStream.WriteAsync(msg);
                            Console.WriteLine($"[Network] >>> Sent: {msg.Latitude}, {msg.Longitude}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Нормальная остановка
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Loop] Error (Disconnect): {ex.Message}");
                    // 3. Логика ПЕРЕПОДКЛЮЧЕНИЯ
                    // Если произошла ошибка, мы просто идем на новый круг while.
                    // Но сначала ждем пару секунд, чтобы не дудосить сервер.
                    await Task.Delay(3000, token);
                }
                finally
                {
                    // Чистим ресурсы перед следующей попыткой
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
                if (location == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                    location = await Geolocation.Default.GetLocationAsync(request);
                }

                if (location != null)
                {
                    HandleLocationUpdate(this, location); // Просто кидаем в тот же буфер
                    Console.WriteLine("[GPS] Initial location queued.");
                }
            }
            catch (Exception ex) { Console.WriteLine($"[GPS] Initial fetch failed: {ex.Message}"); }
        }
    }
}