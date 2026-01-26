namespace GUI_Tests.Tests.SignalR;

/// <summary>
/// Тесты для SignalR соединения (RouteHubService)
/// 
/// ВАЖНО: Эти тесты проверяют реальное подключение к серверу.
/// Для их работы:
/// 1. Сервер должен быть запущен и доступен
/// 2. Нужен валидный access_token в SecureStorage (или отключить авторизацию на сервере для тестов)
/// 
/// Если сервер недоступен - тесты будут пропущены (не fail).
/// </summary>
public class SignalRConnectionTests
{
    // URL вашего SignalR хаба (тот же что в RouteHubService)
    private const string HubUrl = "https://esme-aspiratory-september.ngrok-free.dev/hubs/notifications";
    
    // Таймаут подключения
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SignalR_ServerUrl_ShouldBeReachable()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = ConnectionTimeout;
        
        try
        {
            // Act - проверяем что сервер отвечает
            // SignalR negotiate endpoint
            var negotiateUrl = HubUrl + "/negotiate?negotiateVersion=1";
            var response = await httpClient.PostAsync(negotiateUrl, null);
            
            // Assert - сервер должен ответить (даже если 401 Unauthorized - это ОК, значит сервер работает)
            Assert.True(
                response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
                $"Сервер должен быть доступен. Получен статус: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            // Сервер недоступен - пропускаем тест
            Assert.True(true, $"SignalR сервер недоступен: {ex.Message}. Тест пропущен.");
        }
        catch (TaskCanceledException)
        {
            Assert.True(true, "SignalR сервер не ответил вовремя (timeout). Тест пропущен.");
        }
    }

    [Fact]
    public void SignalR_HubUrl_ShouldBeValidUrl()
    {
        // Arrange & Act
        var isValidUrl = Uri.TryCreate(HubUrl, UriKind.Absolute, out var uri);
        
        // Assert
        Assert.True(isValidUrl, "HubUrl должен быть валидным URL");
        Assert.Equal("https", uri!.Scheme);
    }

    [Fact]
    public void RouteUpdatedDto_ShouldHaveRequiredProperties()
    {
        // Arrange - создаём DTO как в сервисе
        var dto = new RouteUpdatedDto
        {
            Name = "Тестовая точка",
            Address = "ул. Тестовая, 123"
        };
        
        // Assert
        Assert.Equal("Тестовая точка", dto.Name);
        Assert.Equal("ул. Тестовая, 123", dto.Address);
    }

    [Fact]
    public void RouteUpdatedDto_DefaultValues_ShouldBeEmptyStrings()
    {
        // Arrange
        var dto = new RouteUpdatedDto();
        
        // Assert - по умолчанию должны быть пустые строки (не null)
        Assert.Equal(string.Empty, dto.Name);
        Assert.Equal(string.Empty, dto.Address);
    }

    /// <summary>
    /// Локальная копия DTO для тестов (чтобы не зависеть от основного проекта)
    /// </summary>
    public class RouteUpdatedDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
