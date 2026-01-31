using Android.App;
using Android.Content;
using Android.OS;
using LogisticMobileApp.Services;

namespace LogisticMobileApp.Platforms.Android
{
    public class AndroidBackgroundService : IBackgroundService
    {
        public void Start(string driverName, string licensePlate)
        {
            var intent = new Intent(global::Android.App.Application.Context, typeof(LocationForegroundService));
            intent.SetAction("START_SERVICE");
            intent.PutExtra("DriverName", driverName);
            intent.PutExtra("LicensePlate", licensePlate);

            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    // Пытаемся запустить как Foreground
                    global::Android.App.Application.Context.StartForegroundService(intent);
                }
                else
                {
                    global::Android.App.Application.Context.StartService(intent);
                }
            }
            catch (ForegroundServiceStartNotAllowedException ex)
            {
                // ЭТО И ЕСТЬ ВАША ОШИБКА
                // Если Android запретил запускать Foreground Service, 
                // это значит, что приложение считается "фоновым".
                System.Diagnostics.Debug.WriteLine($"[Service Error] Не удалось запустить Foreground Service: {ex.Message}");

                // План Б: Запускаем как обычный сервис (он проживет меньше, но не упадет)
                // Но лучше всего - Решение 1 (Battery Optimization)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Service Error] Общая ошибка: {ex.Message}");
            }
        }

        public void Stop()
        {
            var intent = new Intent(global::Android.App.Application.Context, typeof(LocationForegroundService));
            intent.SetAction("STOP_SERVICE");
            global::Android.App.Application.Context.StartService(intent);
        }
    }
}