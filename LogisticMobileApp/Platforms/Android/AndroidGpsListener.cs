using Android.Content;
using Android.Locations;
using Android.OS;
using LogisticMobileApp.Services.LocationStreaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogisticMobileApp.Services; // Ваш namespace с интерфейсом
using Microsoft.Maui.Devices.Sensors; // Для класса Location
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace LogisticMobileApp.Platforms.Android
{
    public class AndroidGpsListener : Java.Lang.Object, IGpsListener, ILocationListener
    {
        private LocationManager _locationManager;

        // 1. Реализуем переименованное событие
        public event EventHandler<Microsoft.Maui.Devices.Sensors.Location> LocationChanged;

        public void StartListening()
        {
            _locationManager = (LocationManager)Platform.CurrentActivity.GetSystemService(Context.LocationService);
            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 2000, 0, this);
        }

        public void StopListening()
        {
            _locationManager?.RemoveUpdates(this);
        }

        // 2. Метод Android (оставляем имя как требует система)
        public void OnLocationChanged(global::Android.Locations.Location location)
        {
            if (location != null)
            {
                // Явно указываем, что создаем MAUI Location
                var mauiLocation = new Microsoft.Maui.Devices.Sensors.Location(
                    location.Latitude,
                    location.Longitude,
                    location.Altitude);

                // 3. Вызываем СОБЫТИЕ (теперь имена разные, ошибки не будет)
                LocationChanged?.Invoke(this, mauiLocation);
            }
        }

        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string provider, Availability status, Bundle extras) { }
    }
}