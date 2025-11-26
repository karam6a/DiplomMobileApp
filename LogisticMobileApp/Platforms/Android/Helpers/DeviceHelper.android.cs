using Android.OS;
using Android.Provider;
using LogisticMobileApp.Helpers;
using Microsoft.Maui.ApplicationModel; // ← обязательно добавь!

namespace LogisticMobileApp.Platforms.Android.Helpers
{
    public class DeviceHelper : IDeviceHelper
    {
        private const string IdKey = "device_identifier";
        private const string FirstRunKey = "device_helper_first_run_done"; // флаг

        private static string _cachedDeviceId;

        public string GetDeviceName() => Build.Model ?? "Unknown Device";

        public async Task<string> GetOrCreateDeviceIdentifierAsync()
        {
            // Если уже есть в памяти — мгновенно возвращаем
            if (!string.IsNullOrWhiteSpace(_cachedDeviceId))
                return _cachedDeviceId;

            // Проверяем, был ли уже "первый запуск"
            var firstRunDone = await SecureStorage.Default.GetAsync(FirstRunKey);
            bool isFirstRun = string.IsNullOrWhiteSpace(firstRunDone);

            // ЕСЛИ ЭТО ПЕРВЫЙ ЗАПУСК — очищаем старый ID (чтобы точно получить настоящий!)
            if (isFirstRun)
            {
                SecureStorage.Remove(IdKey); // ← вот и очистка!
            }

            // Пробуем взять из хранилища (может быть с прошлого запуска)
            var saved = await SecureStorage.Default.GetAsync(IdKey);
            if (!string.IsNullOrWhiteSpace(saved))
            {
                _cachedDeviceId = saved;
                await SecureStorage.Default.SetAsync(FirstRunKey, "true"); // ставим флаг
                return saved;
            }

            // Первый запуск ИЛИ хранилище пустое — идём за AndroidId
            string androidId = null;
            try
            {
                var context = Platform.CurrentActivity ?? Platform.AppContext;
                androidId = Settings.Secure.GetString(context.ContentResolver, Settings.Secure.AndroidId);
            }
            catch { /* игнорируем */ }

            string finalId;

            if (!string.IsNullOrWhiteSpace(androidId) && androidId != "9774d56d682e549c")
            {
                finalId = androidId; // нормальный AndroidId
            }
            else
            {
                finalId = Guid.NewGuid().ToString("N"); // fallback
            }

            // Сохраняем ID и ставим флаг, что первый запуск прошёл
            await SecureStorage.Default.SetAsync(IdKey, finalId);
            await SecureStorage.Default.SetAsync(FirstRunKey, "true");

            _cachedDeviceId = finalId;
            return finalId;
        }
    }
}