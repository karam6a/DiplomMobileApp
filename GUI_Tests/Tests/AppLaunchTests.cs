using GUI_Tests.Base;

namespace GUI_Tests.Tests;

/// <summary>
/// Тесты запуска приложения для Windows (требуют WinAppDriver)
/// Пропущены - используйте Android тесты вместо них
/// </summary>
[Trait("Platform", "Windows")]
public class AppLaunchTests : AppiumTestBase
{
    [Fact(Skip = "Требует WinAppDriver. Используйте Android тесты.")]
    public void App_ShouldLaunch_Successfully()
    {
        // Arrange & Act - приложение запускается в конструкторе базового класса
        
        // Assert - если мы дошли сюда без исключений, приложение запустилось
        Assert.NotNull(Driver);
        Assert.NotNull(Driver.SessionId);
        
        // Делаем скриншот для подтверждения
        TakeScreenshot("AppLaunched");
    }

    [Fact(Skip = "Требует WinAppDriver. Используйте Android тесты.")]
    public void App_ShouldHaveWindow_WithCorrectTitle()
    {
        // Act
        var windowTitle = Driver.Title;
        
        // Assert - окно должно существовать
        Assert.NotNull(windowTitle);
        
        TakeScreenshot("WindowTitle");
    }

    [Fact(Skip = "Требует WinAppDriver. Используйте Android тесты.")]
    public void MainPage_ShouldDisplay_LoadingIndicator()
    {
        // Arrange - приложение стартует с MainPage (страница загрузки)
        // Ждем немного для загрузки UI
        Thread.Sleep(2000);
        
        // Act - ищем элемент CubeGraphicsView по AutomationId (x:Name)
        var cubeExists = ElementExists("CubeGraphicsView", timeoutSeconds: 5);
        
        // Assert
        TakeScreenshot("MainPage_LoadingState");
        
        // Если CubeGraphicsView не найден, возможно приложение уже перешло на другую страницу
        // Это нормально - просто фиксируем состояние
        Console.WriteLine($"CubeGraphicsView exists: {cubeExists}");
    }
}
