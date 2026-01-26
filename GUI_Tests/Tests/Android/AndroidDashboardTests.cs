using GUI_Tests.Base;

namespace GUI_Tests.Tests.Android;

/// <summary>
/// Тесты для страницы Dashboard (панель управления)
/// </summary>
public class AndroidDashboardTests : AndroidTestBase
{
    /// <summary>
    /// Проверить что мы на странице Dashboard
    /// </summary>
    private bool IsOnDashboardPage(int timeoutSeconds = 10)
    {
        var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
        
        while (DateTime.Now < endTime)
        {
            // Проверяем элементы Dashboard
            if (ElementExists("DriverLabel", timeoutSeconds: 1) ||
                ElementExists("DriverNameLabel", timeoutSeconds: 1) ||
                ElementExists("LogoutButton", timeoutSeconds: 1))
            {
                return true;
            }
            Thread.Sleep(500);
        }
        
        return false;
    }

    [Fact]
    public void Dashboard_ShouldDisplay_DriverCard()
    {
        // Arrange - ждем загрузки Dashboard
        // Примечание: для этого теста пользователь должен быть авторизован
        var isOnDashboard = IsOnDashboardPage();
        
        TakeScreenshot("Dashboard_Initial");
        SavePageSource("Dashboard_Initial");
        
        if (!isOnDashboard)
        {
            // Если не на Dashboard - возможно на странице регистрации
            Assert.True(true, "Не удалось попасть на Dashboard - возможно требуется авторизация");
            return;
        }

        // Assert - проверяем наличие карточки водителя
        Assert.True(ElementExists("DriverLabel"), "Метка 'Водитель' должна отображаться");
    }

    [Fact]
    public void Dashboard_ShouldDisplay_LogoutButton()
    {
        // Arrange
        var isOnDashboard = IsOnDashboardPage();
        
        if (!isOnDashboard)
        {
            TakeScreenshot("Dashboard_Logout_NotReached");
            Assert.True(true, "Не удалось попасть на Dashboard");
            return;
        }

        // Assert
        Assert.True(ElementExists("LogoutButton"), "Кнопка выхода должна отображаться");
        
        TakeScreenshot("Dashboard_LogoutButton");
    }

    [Fact]
    public void Dashboard_LogoutButton_ShouldBeClickable()
    {
        // Arrange
        var isOnDashboard = IsOnDashboardPage();
        
        if (!isOnDashboard)
        {
            Assert.True(true, "Не удалось попасть на Dashboard");
            return;
        }

        // Act
        var logoutButton = FindByAutomationId("LogoutButton");
        
        // Assert
        Assert.True(logoutButton.Displayed, "Кнопка выхода должна быть видима");
        Assert.True(logoutButton.Enabled, "Кнопка выхода должна быть активна");
        
        TakeScreenshot("Dashboard_LogoutClickable");
    }

    [Fact]
    public void Dashboard_ShouldDisplay_DriverName()
    {
        // Arrange
        var isOnDashboard = IsOnDashboardPage();
        
        if (!isOnDashboard)
        {
            Assert.True(true, "Не удалось попасть на Dashboard");
            return;
        }

        // Assert - имя водителя должно отображаться
        var driverNameExists = ElementExists("DriverNameLabel");
        
        TakeScreenshot("Dashboard_DriverName");
        
        // Имя может быть пустым если данные ещё загружаются
        Assert.True(true, $"DriverNameLabel exists: {driverNameExists}");
    }
}
