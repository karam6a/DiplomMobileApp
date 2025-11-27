using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LogisticMobileApp.Models;

namespace LogisticMobileApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        private const string BaseAddress = "https://esme-aspiratory-september.ngrok-free.dev/";
        private const string ClientsEndpoint = "api/Clients";
        private const string ActivateEndpoint = "auth/activate";
        private const string RefreshEndpoint = "auth/refresh";
        private const string DriverMeEndpoint = "api/Drivers/me";

        public ApiService()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(BaseAddress)
            };
        }

        // ← ВОТ ЭТО ГЛАВНОЕ — метод для добавления токена
        private async Task AddAuthorizationHeaderAsync()
        {
            var token = await SecureStorage.Default.GetAsync("access_token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<ClientItem>> GetClientsAsync(CancellationToken ct = default)
        {
            try
            {
                var data = await _http.GetFromJsonAsync<List<ClientItem>>(ClientsEndpoint, ct);
                return data ?? new List<ClientItem>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обращении к API: {ex.Message}");
            }
        }

        public async Task<ActivateResponse> ActivateAsync(ActivateRequest request, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(ActivateEndpoint, request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка активации: {text}");
                }

                var result = await response.Content.ReadFromJsonAsync<ActivateResponse>(cancellationToken: ct);

                if (result == null)
                    throw new Exception("Пустой ответ от сервера");

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"ActivateAsync failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> TryRefreshTokenAsync(CancellationToken ct = default)
        {
            var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");
            var deviceId = await SecureStorage.Default.GetAsync("device_identifier");

            if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(deviceId))
                return false;

            try
            {
                var request = new { refresh_token = refreshToken, device_identifier = deviceId };

                var response = await _http.PostAsJsonAsync(RefreshEndpoint, request, ct);

                if (!response.IsSuccessStatusCode)
                    return false;

                var result = await response.Content.ReadFromJsonAsync<ActivateResponse>(cancellationToken: ct);
                if (result == null) return false;

                // Обновляем токены
                await SecureStorage.Default.SetAsync("access_token", result.access_token);
                await SecureStorage.Default.SetAsync("refresh_token", result.refresh_token);
                await SecureStorage.Default.SetAsync("expires_in", result.expires_in.ToString());

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DriverInfo?> GetCurrentDriverAsync(CancellationToken ct = default)
        {
            try
            {
                // Подставляем токен
                await AddAuthorizationHeaderAsync();

                // Делаем запрос
                var response = await _http.GetAsync(DriverMeEndpoint, ct);

                // ← ВОТ СЮДА ВСТАВЛЯЕМ ПРОВЕРКУ НА 401
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Токен просрочен — пытаемся обновить
                    var refreshed = await TryRefreshTokenAsync();
                    if (refreshed)
                    {
                        // Повторяем запрос с новым токеном
                        return await GetCurrentDriverAsync(ct);
                    }
                    else
                    {
                        // Не удалось обновить — выходим из аккаунта
                        throw new Exception("Сессия истекла. Требуется повторная активация.");
                    }
                }

                // Если не 200 — кидаем ошибку
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка сервера: {response.StatusCode} — {errorText}");
                }

                // Успешно — парсим ответ
                return await response.Content.ReadFromJsonAsync<DriverInfo>(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось загрузить данные водителя: {ex.Message}", ex);
            }
        }
    }
}
