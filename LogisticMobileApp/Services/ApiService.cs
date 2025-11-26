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

        public ApiService()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(BaseAddress)
            };
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
    }
}
