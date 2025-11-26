using System;
using System.Threading.Tasks;
using Android.Provider;
using Android.App;

namespace LogisticMobileApp.Helper
{
    public static class DeviceHelper
    {
        private const string DeviceIdKey = "device_identifier";

        // Имя устройства
        public static string GetDeviceName()
        {
            return Android.OS.Build.Model ?? "UnknownDevice";
        }

        // Уникальный идентификатор устройства
        public static async Task<string> GetOrCreateDeviceIdentifierAsync()
        {
            try
            {
                string androidId = Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Settings.Secure.AndroidId);
                if (!string.IsNullOrWhiteSpace(androidId))
                    return androidId;
            }
            catch
            {
                // fallback к SecureStorage
            }

            // fallback
            var existing = await SecureStorage.GetAsync(DeviceIdKey);
            if (!string.IsNullOrWhiteSpace(existing))
                return existing;

            var newId = Guid.NewGuid().ToString("N");
            await SecureStorage.SetAsync(DeviceIdKey, newId);
            return newId;
        }
    }
}
