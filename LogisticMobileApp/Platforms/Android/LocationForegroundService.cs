using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using LogisticMobileApp.Services.LocationStreaming;

namespace LogisticMobileApp.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Service
    {
        public const string NOTIFICATION_CHANNEL_ID = "1000";
        public const int NOTIFICATION_ID = 1; // ID не должен быть 0!

        public override IBinder OnBind(Intent intent) => null;
        public override void OnCreate()
        {
            base.OnCreate();
            System.Diagnostics.Debug.WriteLine("[ANDROID SERVICE] OnCreate called");
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            System.Diagnostics.Debug.WriteLine("[ANDROID SERVICE] OnStartCommand called");

            try
            {
                if (intent?.Action == "STOP_SERVICE")
                {
                    System.Diagnostics.Debug.WriteLine("[ANDROID SERVICE] Stopping...");
                    StopForeground(true);
                    StopSelfResult(startId);
                    return StartCommandResult.NotSticky;
                }

                // РЕГИСТРАЦИЯ УВЕДОМЛЕНИЯ
                RegisterNotification();
                System.Diagnostics.Debug.WriteLine("[ANDROID SERVICE] Notification Registered");

                if (intent?.Action == "START_SERVICE")
                {
                    var driverName = intent.GetStringExtra("DriverName") ?? "Unknown";
                    System.Diagnostics.Debug.WriteLine($"[ANDROID SERVICE] Starting Logic for {driverName}");

                    var gpsService = MauiApplication.Current.Services.GetService<GpsStreamingService>();
                    Task.Run(async () => await gpsService.StartTrackingAsync(driverName, ""));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ANDROID SERVICE ERROR] {ex.Message}");
                // ВАЖНО: Если упали здесь, сервис умрет
            }

            return StartCommandResult.Sticky;
        }

        private void RegisterNotification()
        {
            try
            {
                CreateNotificationChannel();

                // ВНИМАНИЕ: Используем надежную иконку
                var notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                    .SetContentTitle("Eco Logistics")
                    .SetContentText("GPS работает")
                    .SetSmallIcon(Resource.Mipmap.appicon)
                    .SetPriority(NotificationCompat.PriorityLow)
                    .SetOngoing(true)
                    .Build();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
                }
                else
                {
                    StartForeground(NOTIFICATION_ID, notification);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFICATION ERROR] {ex.Message}");
                throw; // Пробрасываем выше, чтобы увидеть в логах
            }
        }

        
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelName = "Location Tracking";
                // Low importance = без звука, просто висит в шторке
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, channelName, NotificationImportance.Low)
                {
                    Description = "Фоновое отслеживание местоположения"
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
    }
}